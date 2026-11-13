using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Gameplay.Tag;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Components;

namespace UPlayGround.State
{
    /// <summary>
    /// 수면에 떠서 이동하거나 잠수해 카메라 방향으로 헤엄치는 수영 상태.
    /// 수면에서는 평면 이동하고, 하강 버튼을 누르거나 카메라를 아래로 향한 채 앞으로 헤엄치면 잠수한다.
    /// 잠수 중에는 상승(점프)·하강(웅크리기/대시) 버튼이 고정 속도로 깊이를 바꾸고,
    /// 버튼이 없을 때는 카메라 방향대로 이동해 아래를 보고 가면 내려가고 위를 보고 가면 올라온다.
    /// 어느 쪽 입력도 없으면 그 깊이에 머문다.
    /// 진입은 <see cref="PlayerMovementController"/>의 입수 감지가, 이탈은 이 상태가 소유한다.
    /// </summary>
    public sealed class PlayerSwimState : PlayerActorState
    {
        private const float MotionFadeTime = 0.3f;
        private const float GroundProbeUp = 0.1f;
        private const float GroundProbeDistance = 0.5f;
        private const float HopEntryBlockTime = 0.8f;
        private const float HopSurfaceTolerance = 0.3f;
        private const float DiveStartMinElapsed = 0.4f;
        private const float DiveSurfaceTolerance = 0.1f;
        private const float DiveMotionMinSpeed = 0.15f;

        private readonly RaycastHit[] _ledgeHits = new RaycastHit[8];
        private GameplayTag _playingMotion;
        private float _stayTime;
        private float _moveTime;
        private float _surfaceY;
        private float _depth;
        private bool _wasFootIKDisabled;
        private bool _hasFastMotion;
        private bool _hasDiveMotion;

        // 잠수 래치: 수면에서 아래를 보고 헤엄치면 켜지고, 위로 헤엄쳐 수면에 닿으면 꺼진다.
        private bool _isDiving;
        private float _diveElapsed;
        private float _diveIntentTime;
        private float _verticalCommand;
        private Vector3 _diveMoveCommand;
        private float _nextRippleTime;

        /// <summary>잠수 모드인지. 수면 평면 이동과 구분되는 카메라 방향 3D 이동 상태.</summary>
        public bool IsDiving => _isDiving;

        /// <summary>머리가 수면 아래인지. 진입 직후 유예 시간에는 낙하 입수 충격을 보호하려고 false다.</summary>
        public bool IsUnderwater =>
            _stayTime >= playerController.SwimSubmergeGraceTime
            && PlayerSwimRules.IsSubmerged(_depth, playerController.SwimHeadHeight);

        public override ActorStateId StateId => ActorStateId.Swim;
        public override GravityOwnership GravityOwner => GravityOwnership.State;
        protected override ActorStateTag StateTagsCore => ActorStateTag.Locomotion;

        internal PlayerSwimState(ActorMovementController controller) : base(controller) { }

        public override bool CanTransitionState(ActorStateId fromState)
        {
            if (!PlayerSwimRules.CanEnterFrom(fromState) || playerController == null) return false;

            // 모션이 없으면 수영 자세가 깨지므로 진입하지 않는다.
            var animator = gameActor.Animator;
            return animator != null
                && animator.HasMotion(MotionTags.SwimIdle)
                && animator.HasMotion(MotionTags.SwimMove);
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _stayTime = 0f;
            _moveTime = 0f;
            _playingMotion = default;
            _isDiving = false;
            _diveElapsed = 0f;
            _diveIntentTime = 0f;
            _verticalCommand = 0f;
            _diveMoveCommand = Vector3.zero;
            _nextRippleTime = 0f;
            _hasFastMotion = gameActor.Animator.HasMotion(MotionTags.SwimFast);
            _hasDiveMotion = gameActor.Animator.HasMotion(MotionTags.SwimDiveMove)
                             && gameActor.Animator.HasMotion(MotionTags.SwimDiveDescend);
            if (playerController.TryGetWaterDepth(out float depth, out float surfaceY))
            {
                _surfaceY = surfaceY;
                _depth = depth;
                if (FootstepWaterSurface.TryResolveSurface(motor.TransientPosition, out var water, out _) && water.dynamics != null)
                    WaterContactStamps.TryEmit(motor.TransientPosition, water.dynamics.entryStrength);
            }

            // 웅크리기 토글이 켜진 채 물을 벗어나면 지상에서 갑자기 웅크린다.
            playerActor.ClearCrouchInput();

            gameActor.Tags?.AddTag(GameplayTags.State_Swim);
            if (playerActor.FootIK != null)
            {
                _wasFootIKDisabled = playerActor.FootIK.ForceDisabled;
                playerActor.FootIK.ForceDisabled = true;
            }

            // 지면 판정을 켜 두면 부력 높이에서 지면에 붙들려 떠오르지 못한다.
            motor.SetGroundSolvingActivation(false);
            gameActor.Animator.StopUpperBodyOverlay(MotionFadeTime);
            PlaySwimMotion(ResolveMotion());
        }

        public override void OnExit(GameActorState toState)
        {
            motor.SetGroundSolvingActivation(true);
            gameActor.Tags?.RemoveTag(GameplayTags.State_Swim);
            if (playerActor.FootIK != null) playerActor.FootIK.ForceDisabled = _wasFootIKDisabled;
            _isDiving = false;
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            _stayTime += deltaTime;
            _moveTime = playerController.HasMoveInput() ? _moveTime + deltaTime : 0f;

            if (playerController.IsInWaterfallDrop)
            {
                ExitToAirborne();
                return;
            }

            if (!playerController.TryGetWaterDepth(out float depth, out float surfaceY))
            {
                if (HasGroundNearby()) ExitToGround();
                else ExitToAirborne();
                return;
            }

            _surfaceY = surfaceY;
            _depth = depth;
            UpdateDiveState(deltaTime);
            EmitSwimRipple();

            if (playerController.HasJumpInput())
            {
                if (TryHopOutOfWater()) return;
                // 물속에서 쓰이지 않은 점프 입력이 남으면 물을 벗어나는 순간 점프가 발동한다.
                playerActor.ClearJumpInput();
            }
            playerActor.ClearCrouchInput();

            if (!_isDiving
                && _stayTime >= playerController.SwimMinStayTime
                && PlayerSwimRules.ShouldExit(depth, playerController.SwimExitDepth, HasGroundNearby()))
            {
                ExitToGround();
                return;
            }

            PlaySwimMotion(ResolveMotion());
        }

        private void EmitSwimRipple()
        {
            if (_isDiving || !playerController.HasMoveInput() || Time.time < _nextRippleTime) return;
            if (!FootstepWaterSurface.TryResolveSurface(motor.TransientPosition, out var surface, out _) || surface.dynamics == null) return;
            _nextRippleTime = Time.time + surface.dynamics.contactInterval;
            WaterContactStamps.TryEmit(motor.TransientPosition, surface.dynamics.swimStrength);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Vector3 lookDirection = playerController.LookInputVector;
            if (_isDiving)
            {
                // 수직으로만 움직일 때는 평면 입력이 없어 이전 방향을 유지한다.
                Vector3 planar = Vector3.ProjectOnPlane(_diveMoveCommand, motor.CharacterUp);
                lookDirection = planar.sqrMagnitude > 0.0001f ? planar.normalized : Vector3.zero;
            }

            if (lookDirection != Vector3.zero && playerController.SwimRotationSharpness > 0f)
            {
                Vector3 smoothed = Vector3.Slerp(
                    motor.CharacterForward,
                    lookDirection,
                    1 - Mathf.Exp(-playerController.SwimRotationSharpness * deltaTime)).normalized;
                currentRotation = Quaternion.LookRotation(smoothed, motor.CharacterUp);
            }

            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 up = motor.CharacterUp;
            Vector3 planar = Vector3.ProjectOnPlane(currentVelocity, up);
            float vertical = Vector3.Dot(currentVelocity, up);

            // 땅에서 달리던 속도는 물의 저항으로 수렴하므로 입수 순간 급정지하지 않는다.
            Vector3 targetPlanar = _isDiving
                ? Vector3.ProjectOnPlane(_diveMoveCommand, up)
                : playerController.MoveInputVector * ResolveSwimSpeed();
            targetPlanar += Vector3.ProjectOnPlane(playerController.SwimCurrentVelocity, up);
            planar = Vector3.Lerp(planar, targetPlanar, 1 - Mathf.Exp(-playerController.SwimMoveSharpness * deltaTime));

            float targetY = PlayerSwimRules.ResolveFloatHeight(_surfaceY, playerController.SwimFloatDepth, ResolveBobOffset());
            float desiredVertical = PlayerSwimRules.ResolveDiveVerticalSpeed(
                motor.TransientPosition.y, targetY, _surfaceY - playerController.SwimMaxDiveDepth,
                _isDiving ? _verticalCommand : 0f,
                playerController.SwimVerticalSharpness, playerController.SwimMaxVerticalSpeed);
            vertical = Mathf.Lerp(vertical, desiredVertical, 1 - Mathf.Exp(-playerController.SwimVerticalDamping * deltaTime));

            currentVelocity = planar + up * vertical;
        }

        /// <summary>
        /// 잠수 모드의 시작·종료와 이동 요청을 갱신한다. 시작은 임계각 이상 아래를 보고 일정 시간 헤엄칠 때만이라
        /// 수면에서 카메라를 살짝 내려다보는 정도로는 가라앉지 않는다.
        /// </summary>
        private void UpdateDiveState(float deltaTime)
        {
            Vector3 cameraMove = playerController.SwimMoveVector;
            float buttonAxis = playerController.SwimVerticalAxis;

            if (!_isDiving)
            {
                _verticalCommand = 0f;
                bool wantsDive = buttonAxis < 0f
                                 || (Mathf.Approximately(buttonAxis, 0f) && playerController.HasSwimDiveIntent);
                bool hasIntent = _hasDiveMotion && CanStartDive() && wantsDive;
                _diveIntentTime = hasIntent ? _diveIntentTime + deltaTime : 0f;
                if (!hasIntent || _diveIntentTime < playerController.SwimDiveStartHoldTime) return;
                BeginDive();
            }

            _diveElapsed += deltaTime;
            Vector3 up = motor.CharacterUp;
            Vector3 scaledCameraMove = cameraMove * playerController.SwimDiveMoveSpeed;
            // 버튼으로 오르내리는 동안에는 카메라 피치가 수평 속도를 깎지 않도록 평면 입력만 쓴다.
            Vector3 planar = Mathf.Approximately(buttonAxis, 0f)
                ? Vector3.ProjectOnPlane(scaledCameraMove, up)
                : playerController.MoveInputVector * playerController.SwimDiveMoveSpeed;
            _verticalCommand = PlayerSwimRules.ResolveVerticalCommand(
                Vector3.Dot(scaledCameraMove, up), buttonAxis, playerController.SwimButtonVerticalSpeed);
            _diveMoveCommand = planar + up * _verticalCommand;

            // 위로 헤엄쳐 수면 높이에 닿으면 잠수를 끝내고 수면 이동으로 돌아간다.
            bool isAtSurface = _depth <= playerController.SwimFloatDepth + DiveSurfaceTolerance;
            if (_diveElapsed >= DiveStartMinElapsed && isAtSurface && _verticalCommand >= 0f)
                EndDive();
        }

        private bool CanStartDive()
        {
            var stamina = playerActor.Stamina;
            bool isNearSurface = _depth <= playerController.SwimFloatDepth + DiveSurfaceTolerance;
            return isNearSurface && (stamina == null || stamina.CanStartDive);
        }

        private void BeginDive()
        {
            _isDiving = true;
            _diveElapsed = 0f;
            _diveIntentTime = 0f;
        }

        private void EndDive()
        {
            _isDiving = false;
            _diveIntentTime = 0f;
            _verticalCommand = 0f;
            _diveMoveCommand = Vector3.zero;
        }

        private bool IsFastSwimming =>
            _hasFastMotion
            && playerController.HasMoveInput()
            && _moveTime >= playerController.SwimFastStartDelay;

        private float ResolveSwimSpeed() => IsFastSwimming ? playerController.SwimFastSpeed : playerController.SwimSpeed;

        private GameplayTag ResolveMotion()
        {
            if (_isDiving) return ResolveDiveMotion();
            if (!playerController.HasMoveInput()) return MotionTags.SwimIdle;
            return IsFastSwimming ? MotionTags.SwimFast : MotionTags.SwimMove;
        }

        /// <summary>요청 방향에 맞춰 자세를 고른다: 가파른 하강은 머리를 아래로, 수평 이동은 엎드려 헤엄, 그 외는 떠 있기.</summary>
        private GameplayTag ResolveDiveMotion()
        {
            Vector3 planar = Vector3.ProjectOnPlane(_diveMoveCommand, motor.CharacterUp);
            float horizontal = planar.magnitude;
            if (PlayerSwimRules.IsSteepDescent(horizontal, _verticalCommand, playerController.SwimSteepDescentAngle))
                return MotionTags.SwimDiveDescend;
            return horizontal >= DiveMotionMinSpeed ? MotionTags.SwimDiveMove : MotionTags.SwimIdle;
        }

        /// <summary>제자리에서만 출렁여 이동 중과 잠수 중에는 몸이 흔들리지 않는다.</summary>
        private float ResolveBobOffset()
        {
            if (_isDiving || playerController.HasMoveInput() || playerController.SwimBobAmplitude <= 0f) return 0f;
            return Mathf.Sin(Time.time * playerController.SwimBobFrequency * Mathf.PI * 2f) * playerController.SwimBobAmplitude;
        }

        private void PlaySwimMotion(GameplayTag motion)
        {
            if (motion == _playingMotion) return;
            _playingMotion = motion;
            gameActor.Animator.PlayMotion(motion, MotionFadeTime);
        }

        private bool HasGroundNearby()
        {
            Vector3 origin = motor.TransientPosition + motor.CharacterUp * GroundProbeUp;
            return Physics.Raycast(
                origin, -motor.CharacterUp, GroundProbeDistance + GroundProbeUp,
                motor.CollidableLayers & motor.StableGroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void ExitToGround()
        {
            playerController.BlockSwimEntry(playerController.SwimReenterCooldown);
            controller.TransitionToState(playerController.HasMoveInput() ? ActorStateId.GroundMove : ActorStateId.Idle);
        }

        private void ExitToAirborne()
        {
            // 상승 버튼의 남은 입력이 낙하 시작을 점프로 바꾸지 않게 한다.
            playerActor.ClearJumpInput();
            Svc.Input.InputBuffer.ConsumeInput(PlayerAction.Jump);
            playerController.BlockSwimEntry(playerController.SwimReenterCooldown);
            controller.TransitionToState(ActorStateId.Airborne);
            motor.ForceUnground();
        }

        /// <summary>전방 수면 위 턱이 있으면 점프로 올라선다. 계단 오르듯 지면 판정이 없는 수영 중에는 이 경로뿐이다.</summary>
        private bool TryHopOutOfWater()
        {
            // 잠수 중에는 수면까지 올라온 뒤에만 턱을 오를 수 있다.
            if (_isDiving || _depth > playerController.SwimFloatDepth + HopSurfaceTolerance
                || !TryFindLedge(out float ledgeY)) return false;

            Vector3 up = motor.CharacterUp;
            float riseHeight = ledgeY - motor.TransientPosition.y + playerController.SwimLedgeClearance;
            float gravity = Mathf.Abs(controller.Gravity.y) * controller.RiseGravityMultiplier;
            float hopSpeed = PlayerSwimRules.ResolveHopSpeed(riseHeight, gravity);

            playerActor.ClearJumpInput();
            Svc.Input.InputBuffer.ConsumeInput(PlayerAction.Jump);
            playerController.BlockSwimEntry(HopEntryBlockTime);
            if (!controller.TryTransitionToState(ActorStateId.Airborne)) return false;

            controller.QueueVelocityChange(up * hopSpeed + motor.CharacterForward * playerController.SwimLedgeForwardSpeed);
            gameActor.Animator.PlayMotion(MotionTags.Jump, 0.05f);
            return true;
        }

        private bool TryFindLedge(out float ledgeY)
        {
            ledgeY = 0f;
            Vector3 up = motor.CharacterUp;
            float startHeight = playerController.SwimLedgeMaxHeight + 0.1f;
            Vector3 origin = new Vector3(motor.TransientPosition.x, _surfaceY, motor.TransientPosition.z)
                             + motor.CharacterForward * playerController.SwimLedgeProbeDistance
                             + up * startHeight;
            int count = Physics.RaycastNonAlloc(
                origin, -up, _ledgeHits, startHeight + 0.5f,
                motor.CollidableLayers & motor.StableGroundLayers, QueryTriggerInteraction.Ignore);

            float best = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _ledgeHits[i];
                if (hit.collider != null && hit.collider.transform.IsChildOf(gameActor.transform)) continue;
                if (Vector3.Dot(hit.normal, up) < 0.7f || hit.point.y < _surfaceY - 0.1f) continue;
                best = Mathf.Max(best, hit.point.y);
            }

            ledgeY = best;
            return !float.IsNegativeInfinity(best);
        }
    }
}
