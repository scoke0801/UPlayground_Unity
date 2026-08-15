using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (400) 카메라 피벗 오프셋과 이동/지형/공중 LookAhead를 보간한다.
    /// 전투와 락온에서는 수평 선행량을 줄여 대상 프레이밍이 우선되게 한다.
    /// </summary>
    public sealed class OffsetCameraModifier : ICameraModifier
    {
        private Vector3 _lookAheadOffset;
        private Vector3 _lookAheadVelocity;

        public int Priority => 400;

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null || frame.State == null) return;
            if (context.IsInputLocked) return;

            CameraSettings settings = context.Settings;
            CameraState state = frame.State;
            bool isCombat = context.CombatStateProvider?.Invoke() ?? false;
            bool isLockOn = context.LockOn?.IsActive ?? false;

            Vector3 targetOffset = isCombat ? settings.combatOffset : settings.defaultOffset;
            targetOffset += ComputeLookAheadOffset(context, settings, isCombat, isLockOn);

            state.CameraOffset = Vector3.SmoothDamp(
                state.CameraOffset,
                targetOffset,
                ref state.OffsetVelocity,
                settings.offsetSmoothTime);
        }

        private Vector3 ComputeLookAheadOffset(
            CameraContext context,
            CameraSettings settings,
            bool isCombat,
            bool isLockOn)
        {
            Vector3 targetLookAhead = Vector3.zero;
            if (settings.enableLookAhead)
            {
                CameraMotionContext motion = context.Motion;
                Vector3 up = motion.IsAvailable ? motion.Up : Vector3.up;
                Vector3 velocity = motion.IsAvailable
                    ? motion.PlanarVelocity
                    : Vector3.zero;
                float speed = velocity.magnitude;
                if (speed > 0.01f)
                {
                    float factor = Mathf.Clamp01(speed / Mathf.Max(settings.lookAheadSpeedRef, 0.01f));
                    targetLookAhead = velocity.normalized * (factor * settings.lookAheadDistance);
                    float contextMultiplier = isLockOn
                        ? settings.lockOnLookAheadMultiplier
                        : isCombat
                            ? settings.combatLookAheadMultiplier
                            : 1f;
                    targetLookAhead *= Mathf.Clamp01(contextMultiplier);
                }

                if (settings.enableTraversalComposition && motion.IsAvailable && !isLockOn)
                    targetLookAhead += ComputeVerticalLookAhead(context, settings, motion, velocity);
            }

            _lookAheadOffset = Vector3.SmoothDamp(
                _lookAheadOffset,
                targetLookAhead,
                ref _lookAheadVelocity,
                settings.lookAheadSmoothTime);

            return _lookAheadOffset;
        }

        private static Vector3 ComputeVerticalLookAhead(
            CameraContext context,
            CameraSettings settings,
            CameraMotionContext motion,
            Vector3 planarVelocity)
        {
            Vector3 up = motion.Up;
            if (!motion.IsGrounded)
            {
                float verticalSpeed = motion.VerticalSpeed;
                float magnitude = Mathf.Abs(verticalSpeed);
                float maxSpeed = Mathf.Max(
                    settings.airborneEffectStartSpeed + 0.01f,
                    settings.airborneSpeedForMax);
                float factor = Mathf.InverseLerp(settings.airborneEffectStartSpeed, maxSpeed, magnitude);
                float distance = verticalSpeed >= 0f
                    ? settings.airborneRiseLookAhead
                    : -settings.airborneFallLookAhead;
                return up * (distance * factor);
            }

            float planarSpeed = planarVelocity.magnitude;
            if (context.Target == null
                || planarSpeed <= 0.01f
                || settings.groundLookAheadDistance <= 0f)
            {
                return Vector3.zero;
            }

            Vector3 direction = planarVelocity / planarSpeed;
            float speedFactor = Mathf.Clamp01(planarSpeed / Mathf.Max(settings.lookAheadSpeedRef, 0.01f));
            float probeDistance = settings.groundLookAheadDistance * speedFactor;
            float sampledHeight = 0f;
            int sampleCount = 0;

            SampleGroundHeight(context, settings, up, direction, probeDistance * 0.5f, ref sampledHeight, ref sampleCount);
            SampleGroundHeight(context, settings, up, direction, probeDistance, ref sampledHeight, ref sampleCount);

            float heightDelta;
            if (sampleCount > 0)
            {
                heightDelta = sampledHeight / sampleCount;
            }
            else
            {
                float normalUp = Vector3.Dot(motion.GroundNormal, up);
                heightDelta = normalUp > 0.01f
                    ? -Vector3.Dot(motion.GroundNormal, direction * probeDistance) / normalUp
                    : 0f;
            }

            float maxHeight = Mathf.Max(0f, settings.groundLookAheadMaxHeight);
            heightDelta = Mathf.Clamp(
                heightDelta * settings.groundLookAheadStrength,
                -maxHeight,
                maxHeight);
            return up * heightDelta;
        }

        private static void SampleGroundHeight(
            CameraContext context,
            CameraSettings settings,
            Vector3 up,
            Vector3 direction,
            float distance,
            ref float heightSum,
            ref int sampleCount)
        {
            if (distance <= 0.01f)
                return;

            Vector3 targetPosition = context.Target.position;
            Vector3 origin = targetPosition + direction * distance + up * settings.groundProbeHeight;
            float castDistance = settings.groundProbeHeight + settings.groundProbeDepth;
            if (!Physics.Raycast(
                    origin,
                    -up,
                    out RaycastHit hit,
                    castDistance,
                    context.CollisionLayers,
                    QueryTriggerInteraction.Ignore)
                || Vector3.Dot(hit.normal, up) <= 0.1f)
            {
                return;
            }

            heightSum += Vector3.Dot(hit.point - targetPosition, up);
            sampleCount++;
        }
    }
}
