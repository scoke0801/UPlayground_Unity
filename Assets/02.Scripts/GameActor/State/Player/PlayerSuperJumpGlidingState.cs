using KinematicCharacterController;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.State
{
    /// <summary>KCC가 도약·잡기·하강 활공을 소유하고 모든 이탈 경로에서 부착을 해제한다.</summary>
    public sealed class PlayerSuperJumpGlidingState : PlayerActorState
    {
        private enum Phase { Preparation, Rising, Catching, Gliding }
        public const string LaunchMarker = "SuperJump.Launch";
        public const string ContactMarker = "Glider.Contact";
        public const string ContactEndMarker = "Glider.ContactEnd";
        public const string HangMarker = "Glider.HangStart";
        private const int CollisionCapacity = 32;

        private readonly Collider[] _overlaps = new Collider[CollisionCapacity];
        private readonly RaycastHit[] _casts = new RaycastHit[CollisionCapacity];
        private PlayerGliderBinding _binding;
        private PlayerGlidingSettingsSO _settings;
        private PlayerGliderView _view;
        private PlayerEquipment _equipment;
        private MotionSet _phaseMotion;
        private MotionSet _catchMotion;
        private Phase _phase;
        private Vector3 _heading;
        private Vector3 _catchVelocity;
        private Vector3 _previousVolumeCenter;
        private float _launchTime, _contactTime, _contactEndTime, _hangTime, _phaseTimer;
        private float _speed;
        private float _verticalSpeed;
        private bool _hasLaunched, _hasContact, _releaseRequested, _hasLeftGround;
        private bool _wasFootIKDisabled;
        private int _turnPose;
        private float _visualTurn;
        private bool _isBoostRequested;
        private bool _isBoostExhausted;
        private bool _hasUsedAirJump;
        private bool _isAirJumpRequested;

        public override ActorStateId StateId => ActorStateId.SuperJumpGliding;
        public override GravityOwnership GravityOwner => GravityOwnership.State;
        public override bool AllowsUpwardMotionVelocityChange => !_hasContact;
        protected override ActorStateTag StateTagsCore => ActorStateTag.Airborne;
        public bool IsActive { get; private set; }
        public bool IsBoosting { get; private set; }
        public bool ShouldFallFromStaminaExhaustion => IsActive && _hasLaunched
            && playerActor.Stamina is { Current: <= 0f };
        public string LastExitReason { get; private set; }
        public string InterruptedMotionName { get; private set; }

        internal PlayerSuperJumpGlidingState(ActorMovementController controller) : base(controller) { }

        public override bool CanTransitionState(ActorStateId fromState)
        {
            var binding = playerController.GliderBinding;
            return fromState is (ActorStateId.Idle or ActorStateId.GroundMove or ActorStateId.Stop or ActorStateId.TurnInPlace)
                && !playerActor.IsInCombat
                && binding != null && binding.CanUse && motor.GroundingStatus.IsStableOnGround
                && gameActor.Animator.HasMotion(MotionTags.SuperJump)
                && gameActor.Animator.HasMotion(MotionTags.GliderCatch)
                && gameActor.Animator.HasMotion(MotionTags.GliderHang)
                && gameActor.Animator.HasMotion(MotionTags.GliderTurnLeft)
                && gameActor.Animator.HasMotion(MotionTags.GliderTurnRight);
        }

        public override bool BlocksExitTo(GameActorState next) => IsActive && next.StateId is
            ActorStateId.Attack or ActorStateId.Charge or ActorStateId.Dash or ActorStateId.Dodge
            or ActorStateId.Guard or ActorStateId.JumpAttack or ActorStateId.JumpDashAttack
            or ActorStateId.DashAttack or ActorStateId.FinishAttack or ActorStateId.SpecialBreakAttack
            or ActorStateId.Ultimate or ActorStateId.Crouching;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _binding = playerController.GliderBinding;
            _settings = _binding.Settings;
            _equipment = playerActor.GetPlayerEquipment();
            _phase = Phase.Preparation;
            _phaseTimer = 0f;
            _hasLaunched = _hasContact = _releaseRequested = _hasLeftGround = false;
            _hasUsedAirJump = _isAirJumpRequested = false;
            _turnPose = 0;
            _visualTurn = 0f;
            _isBoostRequested = _isBoostExhausted = IsBoosting = false;
            _catchMotion = ResolveCatchMotion();
            _heading = motor.CharacterForward;
            IsActive = true;
            LastExitReason = null;
            InterruptedMotionName = null;
            playerActor.ClearGlidingActionInputs();
            _equipment?.CancelMainWeaponDrawMotionRequest();
            _equipment?.BeginConsumableUseEquipment();
            _binding.BeginTraversal();
            if (playerActor.FootIK != null)
            {
                _wasFootIKDisabled = playerActor.FootIK.ForceDisabled;
                playerActor.FootIK.ForceDisabled = true;
            }
            gameActor.Animator.StopUpperBodyOverlay(_settings.motionBlendTime);
            gameActor.Tags?.AddTag(GameplayTags.State_Airborne);
            PlayPhase(MotionTags.SuperJump);
            if (!TryGetMarkerTime(_phaseMotion, LaunchMarker, out _launchTime)) ReturnToAirborne("도약 마커 누락");
        }

        public override void OnExit(GameActorState toState)
        {
            Cleanup();
            base.OnExit(toState);
        }

        /// <summary>이벤트 구독 없이 현재 실행의 새 점프 입력만 놓기로 받아들인다.</summary>
        public void RequestRelease()
        {
            if (IsActive && _hasContact) _releaseRequested = true;
        }

        /// <summary>새 슈퍼 점프 입력을 한 번만 예약하고 실제 재상승 시점에 비용을 지불한다.</summary>
        public bool TryRequestAirJump()
        {
            if (!IsActive || !_hasLaunched || _hasUsedAirJump || _isAirJumpRequested
                || _releaseRequested || playerActor.IsInputSuppressed || !playerActor.IsAlive()
                || playerActor.IsInCombat || _binding == null || !_binding.CanUse
                || motor.GroundingStatus.IsStableOnGround
                || playerActor.Stamina?.CanSuperJumpInAir != true)
                return false;

            _isAirJumpRequested = true;
            return true;
        }

        /// <summary>대시 홀드를 활공 가속으로 받고, 키를 놓아야 소진 잠금을 해제한다.</summary>
        public void RequestBoost(bool isHeld)
        {
            _isBoostRequested = isHeld;
            if (isHeld) return;
            _isBoostExhausted = false;
            IsBoosting = false;
        }

        /// <summary>지상 달리기와 같은 시작 잔량 기준으로 가속 여부를 결정한다.</summary>
        public void UpdateBoost()
        {
            var stamina = playerActor.Stamina;
            IsBoosting = IsActive && _phase == Phase.Gliding && _isBoostRequested
                && !_isBoostExhausted && !playerActor.IsInputSuppressed && stamina != null
                && (IsBoosting ? stamina.Current > 0f : stamina.CanStartSprint);
        }

        /// <summary>소진 프레임의 가속을 멈추고 상태 갱신에서 낙하로 전환한다.</summary>
        public void ExhaustBoost()
        {
            IsBoosting = false;
            _isBoostExhausted = true;
        }

        /// <summary>늦은 입력과 모델 비활성화가 종료한 실행을 되살리지 못하게 먼저 비활성화한다.</summary>
        public void Cleanup()
        {
            if (!IsActive) return;
            LastExitReason ??= "강제 정리";
            IsActive = false;
            _isAirJumpRequested = false;
            _isBoostRequested = _isBoostExhausted = IsBoosting = false;
            _hasContact = false;
            _binding?.EndTraversal();
            _equipment?.EndConsumableUseEquipment();
            if (playerActor.FootIK != null) playerActor.FootIK.ForceDisabled = _wasFootIKDisabled;
            if (_view != null)
            {
                _view.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(_view.gameObject);
                _view = null;
            }
            playerActor.ClearGlidingActionInputs();
            gameActor.Tags?.RemoveTag(GameplayTags.State_Airborne);
        }

        public override void UpdateState(float deltaTime)
        {
            if (!IsActive) { ReturnToAirborne("비활성화"); return; }
            if (ShouldFallFromStaminaExhaustion)
            {
                // 상승 중 소진되어도 관성으로 고도를 더 얻거나 활공체를 다시 잡지 못하게 한다.
                motor.BaseVelocity = Vector3.ProjectOnPlane(motor.BaseVelocity, motor.CharacterUp)
                    + motor.CharacterUp * Mathf.Min(0f, Vector3.Dot(motor.BaseVelocity, motor.CharacterUp));
                ReturnToAirborne("스태미나 소진");
                return;
            }
            playerActor.ClearGlidingActionInputs();
            if (_binding == null || !_binding.isActiveAndEnabled || playerActor.IsInputSuppressed)
            { ReturnToAirborne("모델 또는 입력 잠금"); return; }
            if (_releaseRequested || (_phase >= Phase.Catching && _view == null))
            { ReturnToAirborne(_releaseRequested ? "수동 놓기" : "탈것 소실"); return; }
            if (gameActor.Animator.CurrentMotionSet != _phaseMotion)
            {
                InterruptedMotionName = gameActor.Animator.CurrentMotionSetName;
                ReturnToAirborne("모션 교체"); return;
            }

            _phaseTimer += deltaTime;
            if (_phase == Phase.Rising && Vector3.Dot(motor.Velocity, motor.CharacterUp) <= 0f
                && _catchMotion == null)
            { ReturnToAirborne("잡기 모션 누락"); return; }
            if (_phase != Phase.Gliding && _phaseTimer > _settings.phaseTimeout)
            { ReturnToAirborne("구간 시간 초과"); return; }
            if (_phase == Phase.Catching && !_hasContact && !UpdateCatchView())
            { ReturnToAirborne("잡기 중 탈것 충돌"); return; }
            if (_phase == Phase.Catching && !_hasContact
                && gameActor.Animator.CurrentMotionSet == _phaseMotion
                && gameActor.Animator.CurrentEvaluatedMotionSetTime >= _contactTime)
            {
                if (!TryAttach() && gameActor.Animator.CurrentEvaluatedMotionSetTime >= _contactEndTime)
                { ReturnToAirborne("접촉 조건 실패"); return; }
            }
            if (_phase == Phase.Catching && _hasContact
                && gameActor.Animator.CurrentEvaluatedMotionSetTime >= _hangTime)
            {
                _phase = Phase.Gliding;
                PlayPhase(MotionTags.GliderHang);
            }
        }

        public override void UpdateRotation(ref Quaternion rotation, float deltaTime)
        {
            if (_hasContact) rotation = Quaternion.LookRotation(_heading, motor.CharacterUp);
        }

        public override void UpdateVelocity(ref Vector3 velocity, float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f) return;
            if (TryApplyAirJump(ref velocity, deltaTime)) return;
            if (_phase == Phase.Preparation)
            {
                if (!motor.GroundingStatus.IsStableOnGround) { ReturnToAirborne("준비 중 지면 이탈"); return; }
                if (gameActor.Animator.CurrentMotionSet != _phaseMotion
                    || gameActor.Animator.CurrentEvaluatedMotionSetTime < _launchTime) return;
                _verticalSpeed = _settings.LaunchSpeed;
                velocity = Vector3.ProjectOnPlane(velocity, motor.CharacterUp)
                    + motor.CharacterUp * PlayerGlidingMovement.IntegrateBallisticStep(
                        ref _verticalSpeed, _settings.jumpGravity, deltaTime);
                _hasLaunched = true;
                playerController.SpendSuperJump();
                motor.ForceUnground();
                _phase = Phase.Rising;
                _phaseTimer = 0f;
                return;
            }

            if (_hasContact) { UpdateGlidingVelocity(ref velocity, deltaTime); return; }
            Vector3 horizontal = Vector3.ProjectOnPlane(velocity, motor.CharacterUp);
            float vertical = _verticalSpeed;
            if (_phase == Phase.Rising)
            {
                Vector3 input = playerController.MoveInputVector;
                if (input.sqrMagnitude > 0f)
                    horizontal = Vector3.MoveTowards(horizontal,
                        input * _settings.ascentSpeedLimit, _settings.ascentAcceleration * deltaTime);
                float catchTime;
                if (TryGetMarkerTime(_catchMotion, ContactMarker, out catchTime)
                    && vertical / _settings.jumpGravity <= catchTime + deltaTime)
                {
                    _catchVelocity = horizontal;
                    BeginCatch();
                }
            }
            else horizontal = _catchVelocity;
            velocity = horizontal + motor.CharacterUp * PlayerGlidingMovement.IntegrateBallisticStep(
                ref _verticalSpeed, _settings.jumpGravity, deltaTime);
        }

        private bool TryApplyAirJump(ref Vector3 velocity, float deltaTime)
        {
            if (!_isAirJumpRequested) return false;
            _isAirJumpRequested = false;
            if (_hasUsedAirJump || _releaseRequested || playerActor.IsInputSuppressed || !playerActor.IsAlive()
                || playerActor.IsInCombat || _binding == null || !_binding.CanUse
                || motor.GroundingStatus.IsStableOnGround
                || playerActor.Stamina?.TrySpendSuperJumpInAir() != true)
                return false;

            _hasUsedAirJump = true;
            _binding.Attach(null);
            if (_view != null)
            {
                _view.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(_view.gameObject);
                _view = null;
            }
            _hasContact = false;
            _isBoostRequested = IsBoosting = false;
            _turnPose = 0;
            _visualTurn = 0f;
            _verticalSpeed = _settings.AirJumpSpeed;
            _phase = Phase.Rising;
            _phaseTimer = 0f;
            PlayPhase(gameActor.Animator.HasMotion(MotionTags.DoubleJump, true)
                ? MotionTags.DoubleJump : MotionTags.Jump);
            motor.ForceUnground();
            // 이전 낙하 속도를 지워 늦게 눌러도 같은 추가 높이를 확보한다.
            velocity = Vector3.ProjectOnPlane(velocity, motor.CharacterUp)
                + motor.CharacterUp * PlayerGlidingMovement.IntegrateBallisticStep(
                    ref _verticalSpeed, _settings.jumpGravity, deltaTime);
            return true;
        }

        private MotionSet ResolveCatchMotion()
        {
            var animator = gameActor.Animator as PlayerActorAnimator;
            var weapon = _equipment != null && _equipment.IsMainWeaponEquipped
                ? _equipment.GetMainWeaponType()
                : UPlayGround.Data.EnumType.WeaponType.NoWeapon;
            return animator?.PlayerMotionSet.GetMotionSet(
                weapon, MotionTags.GliderCatch);
        }

        private void BeginCatch()
        {
            _view = UnityEngine.Object.Instantiate(_binding.Prefab);
            PositionView(PlayerGlidingMovement.GetCatchAnchor(motor.TransientPosition,
                motor.CharacterUp, _verticalSpeed, _settings.jumpGravity), motor.TransientRotation);
            _previousVolumeCenter = _view.transform.TransformPoint(_view.CollisionCenter);
            if (HasObstruction(_previousVolumeCenter, _previousVolumeCenter))
            { ReturnToAirborne("전개 위치 장애물"); return; }
            _phase = Phase.Catching;
            _phaseTimer = 0f;
            PlayPhase(MotionTags.GliderCatch);
            if (!TryGetMarkerTime(_phaseMotion, ContactMarker, out _contactTime)
                || !TryGetMarkerTime(_phaseMotion, ContactEndMarker, out _contactEndTime)
                || !TryGetMarkerTime(_phaseMotion, HangMarker, out _hangTime)
                || _contactEndTime <= _contactTime || _hangTime <= _contactEndTime) ReturnToAirborne("잡기 구간 마커 오류");
        }

        private bool UpdateCatchView()
        {
            // 저프레임에서 접촉 창을 건너뛰어도 탈것을 과거 정점에 남겨두지 않는다.
            PositionView(PlayerGlidingMovement.GetCatchAnchor(motor.TransientPosition,
                motor.CharacterUp, _verticalSpeed, _settings.jumpGravity), motor.TransientRotation);
            Vector3 center = _view.transform.TransformPoint(_view.CollisionCenter);
            if (HasObstruction(_previousVolumeCenter, center)) return false;
            _previousVolumeCenter = center;
            return true;
        }

        private bool TryAttach()
        {
            Vector3 leftPalm = GetPhysicsPalmPosition(_binding.PalmLeft);
            Vector3 rightPalm = GetPhysicsPalmPosition(_binding.PalmRight);
            if (_view == null || Vector3.Distance(motor.TransientPosition, _view.RiderAnchor.position) > _settings.catchRootDistance
                || Quaternion.Angle(motor.TransientRotation, _view.RiderAnchor.rotation) > _settings.catchAngle
                || Vector3.Distance(leftPalm, _view.GripLeft.position) > _settings.catchHandDistance
                || Vector3.Distance(rightPalm, _view.GripRight.position) > _settings.catchHandDistance
                || HasBlockedReach(leftPalm, _view.GripLeft.position)
                || HasBlockedReach(rightPalm, _view.GripRight.position)
                || HasObstruction(_previousVolumeCenter, _view.transform.TransformPoint(_view.CollisionCenter)))
                return false;
            _hasContact = true;
            _verticalSpeed = Mathf.Min(0f, _verticalSpeed);
            _speed = Vector3.ProjectOnPlane(motor.Velocity, motor.CharacterUp).magnitude;
            _heading = motor.CharacterForward;
            // KCC 루트는 그대로 두고 탈것의 기준점만 플레이어에 정렬한다.
            _view.transform.SetParent(motor.transform, true);
            PositionView(motor.transform.position, motor.transform.rotation);
            _binding.Attach(_view);
            return true;
        }

        private Vector3 GetPhysicsPalmPosition(Transform palm) =>
            PlayerGlidingMovement.GetPhysicsPosePosition(palm.position, motor.transform.position,
                motor.transform.rotation, motor.TransientPosition, motor.TransientRotation);

        private void PositionView(Vector3 position, Quaternion rotation)
        {
            Quaternion anchorRotation = Quaternion.Inverse(_view.transform.rotation) * _view.RiderAnchor.rotation;
            Vector3 anchorPosition = _view.transform.InverseTransformPoint(_view.RiderAnchor.position);
            Quaternion viewRotation = rotation * Quaternion.Inverse(anchorRotation);
            _view.transform.SetPositionAndRotation(position - viewRotation * anchorPosition, viewRotation);
        }

        private void UpdateGlidingVelocity(ref Vector3 velocity, float deltaTime)
        {
            Vector3 input = playerController.MoveInputVector;
            Vector3 next = PlayerGlidingMovement.RotateHeading(_heading, input, motor.CharacterUp, _settings.turnSpeed, deltaTime);
            float turn = Vector3.SignedAngle(_heading, next, motor.CharacterUp)
                / (_settings.turnSpeed * deltaTime);
            _heading = next;
            _speed = PlayerGlidingMovement.IntegrateSpeed(_speed, input.magnitude, _settings, deltaTime,
                isBoosting: IsBoosting);
            float verticalStart = Mathf.Min(_verticalSpeed, 0f);
            _verticalSpeed = PlayerGlidingMovement.IntegrateDescent(verticalStart,
                _settings.descentSpeed, _settings.descentBlendTime, deltaTime);
            float vertical = (verticalStart - _verticalSpeed) * _settings.descentBlendTime / deltaTime
                - _settings.descentSpeed;
            velocity = _heading * _speed + motor.CharacterUp * vertical;
            _visualTurn = PlayerGlidingMovement.SmoothTurn(_visualTurn, turn,
                _settings.turnPoseBlendTime, deltaTime);
            _view.SetBank(-_visualTurn * _settings.canopyBankAngle);
            if (_phase != Phase.Gliding) return;
            int pose = PlayerGlidingMovement.SelectTurnPose(_turnPose, _visualTurn,
                _settings.turnPoseThreshold, _settings.turnPoseReleaseThreshold);
            if (pose == _turnPose) return;
            _turnPose = pose;
            PlayPhase(pose == 0 ? MotionTags.GliderHang : pose < 0 ? MotionTags.GliderTurnLeft : MotionTags.GliderTurnRight);
        }

        public override void PostGroundingUpdate(float deltaTime)
        {
            if (!IsActive || !_hasLaunched) return;
            if (!motor.GroundingStatus.IsStableOnGround) _hasLeftGround = true;
            if (!_hasLeftGround || !motor.GroundingStatus.IsStableOnGround) return;
            LastExitReason = "착지";
            playerController.TransitionToState(ActorStateId.Airborne);
        }

        public override void AfterCharacterUpdate(float deltaTime)
        {
            if (!IsActive || _view == null || !_hasContact) return;
            Vector3 center = _view.transform.TransformPoint(_view.CollisionCenter);
            if (HasObstruction(_previousVolumeCenter, center)) { ReturnToAirborne("활공체 외형 충돌"); return; }
            _previousVolumeCenter = center;
        }

        public override void OnMovementHit(Collider collider, Vector3 normal, Vector3 point, ref HitStabilityReport report)
        {
            if (IsActive && _hasLaunched && Vector3.Dot(normal, motor.CharacterUp) < 0f)
                ReturnToAirborne("천장 충돌");
        }

        private bool HasObstruction(Vector3 previous, Vector3 current)
        {
            int count = Physics.OverlapBoxNonAlloc(current, _view.CollisionHalfExtents, _overlaps,
                _view.transform.rotation, motor.CollidableLayers, QueryTriggerInteraction.Ignore);
            if (count == _overlaps.Length) return true;
            for (int i = 0; i < count; i++) if (IsObstacle(_overlaps[i])) return true;
            Vector3 delta = current - previous;
            if (delta.sqrMagnitude <= 0f) return false;
            count = Physics.BoxCastNonAlloc(previous, _view.CollisionHalfExtents, delta.normalized, _casts,
                _view.transform.rotation, delta.magnitude, motor.CollidableLayers, QueryTriggerInteraction.Ignore);
            if (count == _casts.Length) return true;
            for (int i = 0; i < count; i++) if (IsObstacle(_casts[i].collider)) return true;
            return false;
        }

        private bool IsObstacle(Collider collider) => collider != null
            && !collider.transform.IsChildOf(gameActor.transform)
            && !collider.transform.IsChildOf(_view.transform);

        private bool HasBlockedReach(Vector3 hand, Vector3 grip)
        {
            Vector3 delta = grip - hand;
            if (delta.sqrMagnitude <= 0f) return false;
            int count = Physics.RaycastNonAlloc(hand, delta.normalized, _casts,
                delta.magnitude, motor.CollidableLayers, QueryTriggerInteraction.Ignore);
            if (count == _casts.Length) return true;
            for (int i = 0; i < count; i++) if (IsObstacle(_casts[i].collider)) return true;
            return false;
        }

        private void PlayPhase(GameplayTag tag)
        {
            gameActor.Animator.PlayMotion(tag, _settings.motionBlendTime);
            _phaseMotion = gameActor.Animator.CurrentMotionSet;
        }

        private void ReturnToAirborne(string reason)
        {
            LastExitReason = reason;
            if (_hasContact)
            {
                Vector3 vertical = motor.CharacterUp * Mathf.Min(0f, Vector3.Dot(motor.BaseVelocity, motor.CharacterUp));
                motor.BaseVelocity = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(motor.BaseVelocity, motor.CharacterUp),
                    _settings.releaseSpeedLimit) + vertical;
            }
            playerActor.ClearJumpInput();
            Svc.Input?.InputBuffer?.ConsumeInput(UPlayGround.InputDefine.PlayerAction.Jump);
            playerController.TransitionToState(ActorStateId.Airborne);
        }

        /// <summary>도약·접촉 시각은 클립 재생 구간과 속도를 반영한 MotionSet 마커에서 읽는다.</summary>
        public static bool TryGetMarkerTime(MotionSet set, string id, out float time)
        {
            time = 0f;
            if (set?.motions == null) return false;
            foreach (var motion in set.motions)
            {
                if (motion == null) continue;
                if (motion.markers != null) foreach (var marker in motion.markers)
                {
                    if (marker != null && marker.id == id)
                    {
                        time += motion.Duration * marker.normalizedTime;
                        return true;
                    }
                }
                time += motion.Duration;
            }
            return false;
        }
    }
}
