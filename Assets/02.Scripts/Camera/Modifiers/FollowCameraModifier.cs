using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (700) 피벗 추적 위치(SmoothDamp)와 카메라 회전(Slerp)을 산출한다.
    /// posSmoothTime 결정(락온/해제스무딩/LookAtOverride/이펙트 override)과 이펙트의 offset/distance 델타를
    /// 여기서 소비한다. 충돌 보정 이전의 *무충돌* 카메라 위치를 채우며, Collision(800)이 이를 덮어쓴다.
    /// 비스무딩 pivotBase는 frame.PivotBase로 Collision에 전달한다(LockOn.EvaluatePivotOffset 1회 호출 보장).
    /// 원본: InGameCameraMode.EvaluatePose 라인 107-123, 133 + EvaluateCameraPosition(Follow부) + EvaluateCameraRotation
    /// </summary>
    public sealed class FollowCameraModifier : ICameraModifier
    {
        public int Priority => 700;

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null || frame.State == null) return;
            if (context.MainCamera == null || context.Target == null || context.CameraPivot == null) return;

            CameraSettings settings = context.Settings;
            CameraState state = frame.State;
            float deltaTime = frame.DeltaTime;

            // 이펙트 오프셋 델타 (원본 라인 109) — state.CameraOffset에 직접 가산
            state.CameraOffset += frame.Effects.offsetDelta;

            // 이펙트 거리 델타 (원본 라인 107)
            float effectDistance = Mathf.Clamp(state.TargetDistance, settings.minDistance, settings.maxDistance)
                                   + frame.Effects.distanceDelta;

            // posSmoothTime / rotSmoothTime 결정 (원본 라인 116-119)
            bool isLockOn = context.LockOn?.IsActive ?? false;
            float posSmoothTime = frame.Effects.positionSmoothTimeOverride ?? settings.positionSmoothTime;
            if (!isLockOn && !frame.KeepPositionSmoothing && context.LookAtOverride == null
                && !frame.Effects.positionSmoothTimeOverride.HasValue)
                posSmoothTime = 0f;
            float rotSmoothTime = frame.Effects.rotationSmoothTimeOverride ?? settings.rotationSmoothTime;

            // 피벗 기준 위치 (원본 EvaluateCameraPosition 라인 281-303)
            Vector3 lockOnPivotOffset = context.LookAtOverride == null && context.LockOn != null
                ? context.LockOn.EvaluatePivotOffset(deltaTime)
                : Vector3.zero;

            Vector3 pivotBase = context.LookAtOverride != null
                ? context.LookAtOverride.position + context.LookAtOverrideOffset
                : context.Target.position + state.CameraOffset + lockOnPivotOffset;

            if (posSmoothTime <= 0f)
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
                    posSmoothTime);
            }

            Vector3 pivotPosition = state.SmoothPosition;
            frame.PivotBase = pivotBase;

            // 무충돌 카메라 위치 (Collision(800)이 덮어씀)
            Quaternion targetRot = Quaternion.Euler(state.CurrentPitch, state.CurrentYaw, 0f);
            Vector3 camDir = targetRot * Vector3.back;
            Vector3 cameraPosition = pivotPosition + camDir * effectDistance;

            Quaternion cameraRotation = EvaluateCameraRotation(context.MainCamera, state, rotSmoothTime);

            state.CurrentDistance = state.TargetDistance;

            frame.Pose.PivotPosition = pivotPosition;
            frame.Pose.CameraPosition = cameraPosition;
            frame.Pose.CameraRotation = cameraRotation;
            frame.Pose.Yaw = state.CurrentYaw;
            frame.Pose.Pitch = state.CurrentPitch;
            frame.Pose.Distance = state.TargetDistance;
        }

        private static Quaternion EvaluateCameraRotation(Camera mainCamera, CameraState state, float smoothTime)
        {
            Quaternion targetRot = Quaternion.Euler(state.CurrentPitch, state.CurrentYaw, 0f);
            if (mainCamera == null || smoothTime <= 0f)
                return targetRot;

            return Quaternion.Slerp(
                mainCamera.transform.rotation,
                targetRot,
                1f - Mathf.Exp(-10f / smoothTime));
        }
    }
}
