using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 기본 플레이 카메라 모드.
    /// 플레이어 추적, 락온, 거리/FOV, 충돌을 포함한 인게임 포즈를 계산한다.
    /// </summary>
    public class InGameCameraMode : ICameraMode
    {
        private float _frontCameraBlend;
        private float _frontCameraBlendVel;
        private Vector3 _lookAheadOffset;
        private Vector3 _lookAheadVelocity;

        private const float FRONT_BLEND_RETURN_SPEED = 0.12f;
        private const float FRONT_BLEND_PULL_SPEED = 0f;

        public CameraModeType ModeType => CameraModeType.InGame;
        public int Priority => 0;
        public bool AllowsPlayerLookInput => true;
        public bool AllowsZoomInput => true;
        public bool AllowsLockOnInput => true;
        public bool UseCollision => true;

        public void OnEnter(CameraRuntimeContext context, CameraModeEnterParams enterParams)
        {
        }

        public void OnExit(CameraRuntimeContext context)
        {
        }

        public void HandleInput(CameraRuntimeContext context, float deltaTime)
        {
            if (context.Settings == null || context.State == null) return;
            if (InputManager.Instance.CurrentLayer != InputLayer.Level_0) return;
            if (Cursor.visible || context.IsInputLocked) return;

            var input = InputManager.Instance;
            if (input == null) return;

            CameraRigState state = context.State;
            bool isLockOn = context.LockOn?.IsActive ?? false;

            if (!isLockOn && !context.IsAligning)
            {
                if (input.GetAction(InputMapNames.PlayerAction, PlayerAction.Look, out InputAction lookAction))
                {
                    Vector2 look = lookAction.ReadValue<Vector2>();
                    state.CurrentYaw += look.x * context.Settings.rotationSpeed * 0.01f;
                    state.CurrentPitch -= look.y * context.Settings.rotationSpeed * 0.01f;
                    if (look.sqrMagnitude > 0.0001f)
                        context.NotifyManualCameraInput?.Invoke();

                    float slopeOffset = context.ComputeSlopePitchOffset?.Invoke() ?? 0f;
                    float dynamicMin = context.Settings.minVerticalAngle + slopeOffset;
                    state.CurrentPitch = Mathf.Clamp(state.CurrentPitch, dynamicMin, context.Settings.maxVerticalAngle);
                }
            }

            if (input.GetAction(InputMapNames.PlayerAction, PlayerAction.Zoom, out InputAction zoomAction))
            {
                float scroll = zoomAction.ReadValue<Vector2>().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    state.TargetDistance -= scroll * context.Settings.zoomSpeed;
                    state.TargetDistance = Mathf.Clamp(
                        state.TargetDistance,
                        context.Settings.minDistance,
                        context.Settings.maxDistance);
                }
            }
        }

        public CameraRigPose EvaluatePose(CameraRuntimeContext context, float deltaTime, CameraEffectState effectState)
        {
            if (context.MainCamera == null || context.Target == null || context.CameraPivot == null || context.Settings == null)
                return default;

            CameraRigState state = context.State;
            CameraSettings settings = context.Settings;
            bool isCombat = context.CombatStateProvider?.Invoke() ?? false;
            bool skipAuto = context.IsInputLocked || context.LookAtOverride != null;

            UpdateRotationTransition(context, settings, state, deltaTime);
            UpdateLockOn(context, settings, state, skipAuto);
            UpdateCameraAlign(context, settings, state, isCombat, deltaTime);
            UpdateOffsetAndDistance(context, settings, state, isCombat);

            state.CurrentYaw += effectState.yawDelta;
            state.CurrentPitch += effectState.pitchDelta;

            float slopeOffset = context.ComputeSlopePitchOffset?.Invoke() ?? 0f;
            float dynamicMin = settings.minVerticalAngle + slopeOffset;
            state.CurrentPitch = Mathf.Clamp(state.CurrentPitch, dynamicMin, settings.maxVerticalAngle);

            float effectDistance = Mathf.Clamp(state.TargetDistance, settings.minDistance, settings.maxDistance)
                                   + effectState.distanceDelta;
            state.CameraOffset += effectState.offsetDelta;

            bool isLockOn = context.LockOn?.IsActive ?? false;
            float posSmoothTime = effectState.positionSmoothTimeOverride ?? settings.positionSmoothTime;
            if (!isLockOn && context.LookAtOverride == null && !effectState.positionSmoothTimeOverride.HasValue)
                posSmoothTime = 0f;
            float rotSmoothTime = effectState.rotationSmoothTimeOverride ?? settings.rotationSmoothTime;

            Vector3 pivotPosition;
            Vector3 cameraPosition = EvaluateCameraPosition(context, settings, state, posSmoothTime, effectDistance, deltaTime, out pivotPosition);
            Quaternion cameraRotation = EvaluateCameraRotation(context.MainCamera, state, rotSmoothTime);
            cameraPosition += effectState.positionDelta;

            float baseFOV = context.DistanceController?.BaseFOV ?? settings.fovExplore;
            float fov = context.MainCamera.fieldOfView;
            if (Mathf.Abs(effectState.fovDelta) > 0.001f)
                fov = baseFOV + effectState.fovDelta;
            else if (!context.HasActiveEffects)
                fov = baseFOV;

            state.CurrentDistance = state.TargetDistance;

            return new CameraRigPose
            {
                PivotPosition = pivotPosition,
                CameraPosition = cameraPosition,
                CameraRotation = cameraRotation,
                Yaw = state.CurrentYaw,
                Pitch = state.CurrentPitch,
                Distance = state.TargetDistance,
                FieldOfView = fov
            };
        }

        private static void UpdateRotationTransition(
            CameraRuntimeContext context,
            CameraSettings settings,
            CameraRigState state,
            float deltaTime)
        {
            if (context.RotationTransition == null) return;

            context.RotationTransition.Update(
                deltaTime,
                settings.minVerticalAngle,
                settings.maxVerticalAngle,
                ref state.CurrentYaw,
                ref state.CurrentPitch);

            if (!context.RotationTransition.IsActive && context.RotationTransition.UnlockOnComplete)
            {
                context.IsInputLocked = false;
                context.RotationTransition.Cancel();
            }
        }

        private static void UpdateLockOn(
            CameraRuntimeContext context,
            CameraSettings settings,
            CameraRigState state,
            bool skipAuto)
        {
            if (context.LockOn == null) return;

            bool needAlign = context.LockOn.UpdateTransition(ref state.CurrentYaw, ref state.CurrentPitch, skipAuto);
            if (needAlign)
            {
                context.StartCameraAlign?.Invoke();
                context.IsAligning = true;
                context.AlignTimer = settings.alignDuration;
            }

            context.LockOn.UpdateRotation(ref state.CurrentYaw, ref state.CurrentPitch, skipAuto);
        }

        private static void UpdateCameraAlign(
            CameraRuntimeContext context,
            CameraSettings settings,
            CameraRigState state,
            bool isCombat,
            float deltaTime)
        {
            if (context.IsInputLocked || !context.IsAligning || context.Target == null) return;

            context.AlignTimer -= deltaTime;
            if (context.AlignTimer <= 0f)
            {
                context.IsAligning = false;
                return;
            }

            Vector3 fwd = context.Target.forward;
            float targetYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
            float targetPitch = isCombat ? settings.combatPitch : settings.explorePitch;

            state.CurrentYaw = Mathf.LerpAngle(state.CurrentYaw, targetYaw, deltaTime * settings.alignSpeed);
            state.CurrentPitch = Mathf.Lerp(state.CurrentPitch, targetPitch, deltaTime * settings.alignSpeed);

            float slopeOffset = context.ComputeSlopePitchOffset?.Invoke() ?? 0f;
            float dynamicMin = settings.minVerticalAngle + slopeOffset;
            state.CurrentPitch = Mathf.Clamp(state.CurrentPitch, dynamicMin, settings.maxVerticalAngle);
        }

        private void UpdateOffsetAndDistance(
            CameraRuntimeContext context,
            CameraSettings settings,
            CameraRigState state,
            bool isCombat)
        {
            if (!context.IsInputLocked)
            {
                bool isLockOn = context.LockOn?.IsActive ?? false;
                Vector3 targetOffset = isCombat ? settings.combatOffset : settings.defaultOffset;
                if (isLockOn)
                    targetOffset += ComputeLookAheadOffset(context, settings);
                else
                    ResetLookAheadOffset();

                state.CameraOffset = Vector3.SmoothDamp(
                    state.CameraOffset,
                    targetOffset,
                    ref state.OffsetVelocity,
                    settings.offsetSmoothTime);
            }

            if (!context.IsInputLocked && context.DistanceController != null)
            {
                bool isLockOn = context.LockOn?.IsActive ?? false;
                context.DistanceController.UpdateFOV(isLockOn, isCombat);
                float dist = context.DistanceController.EvaluateDistance(isLockOn, isCombat, state.TargetDistance);
                if (dist >= 0f)
                    state.TargetDistance = dist;
            }
        }

        private Vector3 EvaluateCameraPosition(
            CameraRuntimeContext context,
            CameraSettings settings,
            CameraRigState state,
            float smoothTime,
            float desiredDistance,
            float deltaTime,
            out Vector3 pivotPosition)
        {
            Vector3 pivotBase = context.LookAtOverride != null
                ? context.LookAtOverride.position + context.LookAtOverrideOffset
                : context.Target.position + state.CameraOffset;

            if (smoothTime <= 0f)
            {
                state.SmoothPosition = pivotBase;
                state.PositionVelocity = Vector3.zero;
            }
            else
            {
                state.SmoothPosition = Vector3.SmoothDamp(
                    state.SmoothPosition,
                    pivotBase,
                    ref state.PositionVelocity,
                    smoothTime);
            }

            pivotPosition = state.SmoothPosition;

            Quaternion rotation = Quaternion.Euler(state.CurrentPitch, state.CurrentYaw, 0f);
            Vector3 camDir = rotation * Vector3.back;

            float finalDist = context.Collision != null
                ? context.Collision.Evaluate(pivotPosition, camDir, desiredDistance)
                : desiredDistance;

            Vector3 backPos = pivotPosition + camDir * finalDist;
            backPos = ResolveSafeBackPosition(context, settings, pivotBase, backPos);
            backPos = ResolveGroundPenetration(context, settings, pivotPosition, camDir, backPos);
            context.Collision?.ApplyFloorRescue(pivotPosition, ref backPos, deltaTime);
            return ResolveFrontCameraBlend(context, settings, pivotPosition, camDir, backPos);
        }

        private Vector3 ComputeLookAheadOffset(CameraRuntimeContext context, CameraSettings settings)
        {
            Vector3 targetLookAhead = Vector3.zero;
            if (settings.enableLookAhead && context.PlayerVelocityProvider != null)
            {
                Vector3 velocity = Vector3.ProjectOnPlane(context.PlayerVelocityProvider.Invoke(), Vector3.up);
                float speed = velocity.magnitude;
                if (speed > 0.01f)
                {
                    float factor = Mathf.Clamp01(speed / Mathf.Max(settings.lookAheadSpeedRef, 0.01f));
                    targetLookAhead = velocity.normalized * (factor * settings.lookAheadDistance);

                    if (context.LockOn?.IsActive ?? false)
                        targetLookAhead *= settings.lockOnLookAheadMultiplier;
                }
            }

            _lookAheadOffset = Vector3.SmoothDamp(
                _lookAheadOffset,
                targetLookAhead,
                ref _lookAheadVelocity,
                settings.lookAheadSmoothTime);

            _lookAheadOffset.y = 0f;
            return _lookAheadOffset;
        }

        private void ResetLookAheadOffset()
        {
            _lookAheadOffset = Vector3.zero;
            _lookAheadVelocity = Vector3.zero;
        }

        private static Vector3 ResolveSafeBackPosition(
            CameraRuntimeContext context,
            CameraSettings settings,
            Vector3 pivotBase,
            Vector3 backPos)
        {
            Vector3 toCam = backPos - pivotBase;
            float toCamDist = toCam.magnitude;
            if (toCamDist <= 0.01f) return backPos;

            Vector3 toCamDir = toCam / toCamDist;
            if (Physics.SphereCast(pivotBase, settings.cameraRadius, toCamDir,
                    out RaycastHit safeHit, toCamDist, context.CollisionLayers))
            {
                if (safeHit.transform != context.Target && !safeHit.transform.IsChildOf(context.Target))
                {
                    float safeDist = Mathf.Max(safeHit.distance - settings.collisionOffset, 0f);
                    backPos = pivotBase + toCamDir * safeDist;
                }
            }

            return backPos;
        }

        private static Vector3 ResolveGroundPenetration(
            CameraRuntimeContext context,
            CameraSettings settings,
            Vector3 pivotPosition,
            Vector3 camDir,
            Vector3 backPos)
        {
            const float CHECK_HEIGHT = 20f;
            const float CHECK_DIST = 40f;
            Vector3 checkOrigin = new Vector3(backPos.x, backPos.y + CHECK_HEIGHT, backPos.z);
            if (!Physics.Raycast(checkOrigin, Vector3.down, out RaycastHit groundHit, CHECK_DIST, context.CollisionLayers))
                return backPos;

            float minY = groundHit.point.y + settings.collisionOffset;
            if (backPos.y >= minY) return backPos;

            if (Mathf.Abs(camDir.y) > 0.001f)
            {
                float groundDist = (minY - pivotPosition.y) / camDir.y;
                float curDist = Vector3.Distance(pivotPosition, backPos);
                if (groundDist >= settings.minDistance && groundDist <= curDist)
                    backPos = pivotPosition + camDir * groundDist;
            }
            else
            {
                backPos.y = minY;
            }

            return backPos;
        }

        private Vector3 ResolveFrontCameraBlend(
            CameraRuntimeContext context,
            CameraSettings settings,
            Vector3 pivotPosition,
            Vector3 camDir,
            Vector3 backPos)
        {
            ComputeCapsuleClearance(context.CharacterCapsule, settings, pivotPosition, camDir,
                out float backClearance, out float frontClearance);

            float backDist = Vector3.Distance(pivotPosition, backPos);
            float targetBlend = backDist < backClearance ? 1f : 0f;
            float blendSpeed = targetBlend > _frontCameraBlend
                ? FRONT_BLEND_PULL_SPEED
                : FRONT_BLEND_RETURN_SPEED;

            _frontCameraBlend = blendSpeed > 0f
                ? Mathf.SmoothDamp(_frontCameraBlend, targetBlend, ref _frontCameraBlendVel, blendSpeed)
                : targetBlend;

            float frontDist = Mathf.Max(frontClearance, 0.3f);
            Vector3 frontPos = pivotPosition + (-camDir) * frontDist;
            return Vector3.Lerp(backPos, frontPos, _frontCameraBlend);
        }

        private static Quaternion EvaluateCameraRotation(Camera mainCamera, CameraRigState state, float smoothTime)
        {
            Quaternion targetRot = Quaternion.Euler(state.CurrentPitch, state.CurrentYaw, 0f);
            if (mainCamera == null || smoothTime <= 0f)
                return targetRot;

            return Quaternion.Slerp(
                mainCamera.transform.rotation,
                targetRot,
                1f - Mathf.Exp(-10f / smoothTime));
        }

        private static void ComputeCapsuleClearance(
            CapsuleCollider capsule,
            CameraSettings settings,
            Vector3 pivotPos,
            Vector3 camDir,
            out float backClearance,
            out float frontClearance)
        {
            backClearance = 0f;
            frontClearance = 0f;

            if (capsule == null) return;

            Transform t = capsule.transform;
            Vector3 worldCenter = t.TransformPoint(capsule.center);
            Vector3 scale = t.lossyScale;

            Vector3 axisLocal;
            float rScale, hScale;
            switch (capsule.direction)
            {
                case 0:
                    axisLocal = Vector3.right;
                    rScale = Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                    hScale = Mathf.Abs(scale.x);
                    break;
                case 2:
                    axisLocal = Vector3.forward;
                    rScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                    hScale = Mathf.Abs(scale.z);
                    break;
                default:
                    axisLocal = Vector3.up;
                    rScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                    hScale = Mathf.Abs(scale.y);
                    break;
            }

            Vector3 axisWorld = t.TransformDirection(axisLocal).normalized;
            float radius = capsule.radius * rScale;
            float halfCyl = Mathf.Max(0f, capsule.height * hScale * 0.5f - radius);

            Vector3 p2c = pivotPos - worldCenter;
            float tOnAxis = Mathf.Clamp(Vector3.Dot(p2c, axisWorld), -halfCyl, halfCyl);
            Vector3 nearestCenter = worldCenter + tOnAxis * axisWorld;

            float effectiveR = radius + settings.cameraRadius;
            Vector3 oc = pivotPos - nearestCenter;
            float halfB = Vector3.Dot(oc, camDir);
            float cVal = oc.sqrMagnitude - effectiveR * effectiveR;
            float disc = halfB * halfB - cVal;

            if (disc < 0f) return;

            float sqrtDisc = Mathf.Sqrt(disc);
            float t1 = -halfB - sqrtDisc;
            float t2 = -halfB + sqrtDisc;

            backClearance = Mathf.Max(t2, 0f);
            frontClearance = Mathf.Max(-t1, 0f);
        }
    }
}
