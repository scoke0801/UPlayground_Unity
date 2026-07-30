using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.State
{
    /// <summary>
    /// 지면 이동 상태 - 걷기/달리기
    /// </summary>
    public class PlayerGroundMoveState : PlayerActorState
    {
        public override string StateName => "GroundMove";
        protected override ActorStateTag StateTagsCore => ActorStateTag.Locomotion;
        
        private float _sprintTimer;
        private float _sprintAutoChangeDelay;
        private float _locomotionPlayRate = 1f;

        private BaseMoveAnimType _cachedAnimType = BaseMoveAnimType.Run;
        
        public PlayerGroundMoveState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Tags?.AddTag(GameplayTags.State_Move);

            _sprintTimer = 0f;
            _sprintAutoChangeDelay = playerActor.PlayerController.SprintAutoStartDelay;

            _cachedAnimType = gameActor.MoveAnimType;
            gameActor.Animator.PlayMotion(GetMoveAnimKey(), 0.15f);
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Tags?.RemoveTag(GameplayTags.State_Move);
            gameActor.Tags?.RemoveTag(GameplayTags.State_Sprint);
            gameActor.Animator.MotionTimelineSpeed = 1f;
            gameActor.Animator.Speed = gameActor.LocalTimeScale;

            // 이동 타입은 입력/대시 연계가 소유한다. 공격·회피 진입만으로
            // Sprint를 해제하지 않아 복귀 뒤 자동 Sprint 재대기가 생기지 않는다.
            base.OnExit(toState);
        }
        public override void UpdateState(float deltaTime)
        {
            // 점프 입력이 있으면 Airborne 상태로 전환
            if (playerController.HasJumpInput())
            {
                playerController.TransitionToState(new PlayerAirborneState(controller));
                return;
            }

            if (playerController.HasDodgeInput())
            {
                playerController.TransitionToState(new PlayerDodgeState(playerController));
                return;
            }

            if (playerController.HasDashInput())
            {
                if (controller.TryTransitionToState(new PlayerDashState(controller)))
                {
                    return;
                }
            }

            // 웅크리기 입력이 있으면 Crouching 상태로 전환
            if (playerController.HasCrouchInput())
            {
                playerController.TransitionToState(new PlayerCrouchingState(controller));
                return;
            }

            // 지면에서 떨어지면 Airborne 상태로 전환 (유예 시간 적용)
            if (ShouldTransitionToAirborne(deltaTime))
            {
                playerController.TransitionToState(new PlayerAirborneState(controller));
                return;
            }

            if (playerController.HasInteractInput())
            {
                PlayerCombat combat = playerActor.GetCombat();
                Transform breakTarget = combat != null ? combat.FindSpecialBreakAttackTarget() : null;
                if (breakTarget != null)
                {
                    Svc.Input.InputBuffer.ConsumeInput(PlayerAction.Interact);
                    playerController.TransitionToState(new PlayerSpecialBreakAttackState(playerController, breakTarget));
                    return;
                }

                if (!playerActor.CanStartInteraction())
                    return;

                playerController.TransitionToState(new PlayerInteractionState(playerController));
                return;
            }

            // 이동 입력이 없으면 Stop 전환 시도 — 전방 기본 클립이 등록된 경우에만 Stop 상태 사용
            if (!playerController.HasMoveInputBuffered())
            {
                float planarSpeed = Vector3.ProjectOnPlane(
                    motor.Velocity,
                    motor.CharacterUp).magnitude;
                Vector3 stopDirection = playerController.LastMoveDirection;
                float stopAngle = stopDirection.sqrMagnitude > 0.0001f
                    ? Vector3.SignedAngle(
                        motor.CharacterForward,
                        stopDirection,
                        motor.CharacterUp)
                    : 0f;
                var directionalStopKey = PlayerStopState.GetStopAnimKey(
                    gameActor.MoveAnimType,
                    stopAngle);
                var forwardStopKey = PlayerStopState.GetStopAnimKeyForward(
                    gameActor.MoveAnimType);
                bool hasStop = gameActor.Animator.HasMotion(directionalStopKey, true)
                               || gameActor.Animator.HasMotion(forwardStopKey, true);

                GameActorState nextState = hasStop && planarSpeed >= controller.MinStopSpeed
                    ? new PlayerStopState(
                        controller,
                        gameActor.MoveAnimType,
                        stopDirection)
                    : new PlayerIdleState(controller);
                if (controller.TryTransitionToState(nextState))
                {
                    return;
                }
            }

            if (playerController.HasGuardInput())
            {
                playerController.TransitionToState(new PlayerGuardState(playerController));
                return;
            }

            if (Svc.Input.InputBuffer.HasInput(PlayerAction.Attack))
            {
                if (PlayerAttackState.TryEnter(playerController))
                    return;
            }

            // 차지 공격: 홀드 임계값 초과 시 우선 진입 (HeavyAttack 버퍼 체크보다 앞)
            if (playerController.IsChargeAttackHeld())
            {
                playerController.TransitionToState(new PlayerChargeState(playerController));
                return;
            }

            if (Svc.Input.InputBuffer.HasInput(PlayerAction.HeavyAttack))
            {
                if (gameActor.MoveAnimType == BaseMoveAnimType.Sprint
                    && playerController.TryTransitionToState(new PlayerDashAttackState(playerController)))
                {
                    Svc.Input.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack);
                    return;
                }

                // TryEnter 가 OnEnter 안에서 HeavyAttack 입력을 소비한다.
                if (PlayerAttackState.TryEnter(playerController))
                    return;
            }


            var skillGauge = playerActor.SkillGauge;
            for (int i = 0; i < PlayerAbilityResourceView.SkillSlotCount; i++)
            {
                if (skillGauge == null) break;
                if (!playerController.HasSkillInput(i)) continue;
                if (skillGauge.CanUseSkill(i) == false) continue;

                if (PlayerAttackState.TryEnter(playerController)) return;
            }

            if (TryEnterTurnInPlace())
                return;
            
            if (_cachedAnimType != gameActor.MoveAnimType)
            {
                _cachedAnimType = gameActor.MoveAnimType;
                gameActor.Animator.PlayMotion(GetMoveAnimKey(), 0.15f);
            }

            if (gameActor.MoveAnimType == BaseMoveAnimType.Run
                && playerController.AutoSprintArmed)
            {
                _sprintTimer += deltaTime;
                if (_sprintTimer >= _sprintAutoChangeDelay)
                {
                    gameActor.MoveAnimType = BaseMoveAnimType.Sprint;
                    gameActor.Tags?.AddTag(GameplayTags.State_Sprint);
                    playerController.SetAutoSprintArmed(false);
                }
            }
            else
            {
                _sprintTimer = 0f;
            }

            UpdateLocomotionPlaybackSpeed(deltaTime);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Vector3 lookDirection = playerController.LookInputVector;
            
            if (lookDirection != Vector3.zero && controller.OrientationSharpness > 0f)
            {
                float planarSpeed = Vector3.ProjectOnPlane(
                    motor.Velocity,
                    motor.CharacterUp).magnitude;
                float speedRatio = Mathf.Clamp01(
                    planarSpeed / Mathf.Max(0.01f, controller.MaxSprintMoveSpeed));
                float sharpness = Mathf.Lerp(
                    controller.OrientationSharpness,
                    controller.OrientationSharpness * controller.SprintOrientationScale,
                    speedRatio);

                // 이동 속도가 높을수록 선회율을 낮춰 뱅킹 호를 만든다.
                Vector3 smoothedLookInputDirection = Vector3.Slerp(
                    motor.CharacterForward, 
                    lookDirection, 
                    1 - Mathf.Exp(-sharpness * deltaTime)).normalized;
                
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, motor.CharacterUp);
            }

            currentRotation = currentRotation.normalized;
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround)
            {
                // 경사로 이동 보정: 현재 속도를 지면 기울기에 맞게 재지향
                currentVelocity = motor.GetDirectionTangentToSurface(
                    currentVelocity, 
                    motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
            
                // 지면 노멀을 고려한 타겟 속도 계산
                Vector3 moveInputVector = playerController.MoveInputVector;
                Vector3 inputRight = Vector3.Cross(moveInputVector, motor.CharacterUp);
                Vector3 reorientedInput = Vector3.Cross(
                    motor.GroundingStatus.GroundNormal, 
                    inputRight).normalized * moveInputVector.magnitude;
            
                Vector3 targetMovementVelocity = reorientedInput * GetMaxMovementSpeed();
            
                float currentPlanarSpeed = Vector3.ProjectOnPlane(
                    currentVelocity,
                    motor.CharacterUp).magnitude;
                float targetPlanarSpeed = targetMovementVelocity.magnitude;
                float sharpness;
                if (Vector3.Dot(currentVelocity, targetMovementVelocity) < 0f)
                    sharpness = ResolveSharpness(controller.TurnDampSharpness);
                else if (targetPlanarSpeed > currentPlanarSpeed)
                    sharpness = ResolveSharpness(controller.AccelerationSharpness);
                else
                    sharpness = ResolveSharpness(controller.DecelerationSharpness);

                currentVelocity = Vector3.Lerp(
                    currentVelocity, 
                    targetMovementVelocity, 
                    1 - Mathf.Exp(-sharpness * deltaTime));
            }
        }

        private bool TryEnterTurnInPlace()
        {
            if (!playerController.HasMoveInput()
                || !playerController.CanEnterTurnInPlace)
            {
                return false;
            }

            Vector3 planarVelocity = Vector3.ProjectOnPlane(
                motor.Velocity,
                motor.CharacterUp);
            float planarSpeed = planarVelocity.magnitude;
            Vector3 targetDirection = Vector3.ProjectOnPlane(
                playerController.MoveInputVector,
                motor.CharacterUp);
            if (planarSpeed < controller.MinTurnSpeed
                || targetDirection.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            targetDirection.Normalize();
            Vector3 movementDirection = planarVelocity / planarSpeed;
            Vector3 previousInputDirection = Vector3.ProjectOnPlane(
                playerController.PreviousMoveDirection,
                motor.CharacterUp);
            if (previousInputDirection.sqrMagnitude > 0.0001f)
                previousInputDirection.Normalize();

            float velocityTurnAngle = Vector3.Angle(
                movementDirection,
                targetDirection);
            float inputTurnAngle = previousInputDirection.sqrMagnitude > 0.0001f
                ? Vector3.Angle(previousInputDirection, targetDirection)
                : 0f;
            Vector3 turnSourceDirection = inputTurnAngle > velocityTurnAngle
                ? previousInputDirection
                : movementDirection;
            float triggerAngle = Mathf.Max(velocityTurnAngle, inputTurnAngle);
            if (triggerAngle < controller.TurnTriggerAngle)
                return false;

            float signedAngle = Vector3.SignedAngle(
                turnSourceDirection,
                targetDirection,
                motor.CharacterUp);
            var turnKey = PlayerTurnInPlaceState.GetTurnAnimKey(
                gameActor.MoveAnimType,
                signedAngle);
            if (!gameActor.Animator.HasMotion(turnKey, true))
                return false;

            return controller.TryTransitionToState(
                new PlayerTurnInPlaceState(
                    controller,
                    gameActor.MoveAnimType,
                    targetDirection,
                    turnSourceDirection: turnSourceDirection));
        }

        private void UpdateLocomotionPlaybackSpeed(float deltaTime)
        {
            var moveKey = GetMoveAnimKey();
            float referenceSpeed =
                gameActor.Animator.MotionSet != null
                && gameActor.Animator.MotionSet.TryGetMotionReferenceSpeed(
                    moveKey,
                    out float bakedSpeed)
                    ? bakedSpeed
                    : controller.GetReferenceClipSpeed(gameActor.MoveAnimType);
            if (referenceSpeed <= 0.001f)
            {
                gameActor.Animator.MotionTimelineSpeed = 1f;
                gameActor.Animator.Speed = gameActor.LocalTimeScale;
                return;
            }

            float planarSpeed = Vector3.ProjectOnPlane(
                motor.Velocity,
                motor.CharacterUp).magnitude;
            float min = Mathf.Min(
                controller.LocomotionPlayRateMin,
                controller.LocomotionPlayRateMax);
            float max = Mathf.Max(
                controller.LocomotionPlayRateMin,
                controller.LocomotionPlayRateMax);
            float targetPlayRate = Mathf.Clamp(
                planarSpeed / referenceSpeed,
                min,
                max);
            float blend = 1f - Mathf.Exp(-12f * Mathf.Max(0f, deltaTime));
            _locomotionPlayRate = Mathf.Lerp(
                _locomotionPlayRate,
                targetPlayRate,
                blend);
            // Graph.Speed만 바꾸면 MotionSet의 _globalTime과 실제 포즈 시간이 갈라져
            // LoopSelf 경계에서 클립 중간 포즈가 시작점으로 튄다.
            // 로코모션 배율은 타임라인에 적용해 포즈와 Section 경계 시계를 함께 조절한다.
            gameActor.Animator.MotionTimelineSpeed = _locomotionPlayRate;
            gameActor.Animator.Speed = gameActor.LocalTimeScale;
        }

        private float ResolveSharpness(float configured)
        {
            return configured > 0f ? configured : controller.StableMovementSharpness;
        }

        private float GetMaxMovementSpeed()
        {
            return controller.GetMaxMoveSpeed(gameActor.MoveAnimType);
        }
        private UPlayGround.Gameplay.Tag.GameplayTag GetMoveAnimKey()
        {
            switch (gameActor.MoveAnimType)
            {
                case BaseMoveAnimType.Walk: return UPlayGround.Data.Actor.Animation.MotionTags.Walk;
                case BaseMoveAnimType.Sprint: return UPlayGround.Data.Actor.Animation.MotionTags.Sprint;
                case BaseMoveAnimType.Run: return UPlayGround.Data.Actor.Animation.MotionTags.Run;
                default: break;
            }

            // 기본은 달리기
            return UPlayGround.Data.Actor.Animation.MotionTags.Run;
        }
    }
}
