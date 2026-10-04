using System;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>명중 요청을 대표 대상 하나로 묶고 화면 경계까지 제한된 수평 보정만 적용한다.</summary>
    public sealed class CameraHitAssist
    {
        /// <summary>명중 시점의 대상과 보정 정책. 피격 위치는 대상의 로컬 좌표로 보존한다.</summary>
        public readonly struct Request
        {
            public readonly Transform Target;
            public readonly Vector3 LocalFocus;
            public readonly float Strength;
            public readonly float MaxAngle;
            public readonly float Duration;
            public readonly float ManualInputDelay;

            public Request(Transform target, Vector3 focus, float strength, float maxAngle,
                float duration, float manualInputDelay)
            {
                Target = target;
                LocalFocus = target != null ? target.InverseTransformPoint(focus) : Vector3.zero;
                Strength = Mathf.Clamp01(strength);
                MaxAngle = Mathf.Clamp(maxAngle, 0f, 180f);
                Duration = Mathf.Max(0.01f, duration);
                ManualInputDelay = Mathf.Max(0f, manualInputDelay);
            }
        }

        // 같은 프레임의 광역 타격 수와 무관하게 할당량과 대상 평가량을 제한한다.
        private const int RequestCapacity = 32;
        private readonly Request[] _pending = new Request[RequestCapacity];
        private int _pendingCount;
        private Request _active;
        private Transform _heldTarget;
        private float _holdRemaining;
        private float _intervalRemaining;
        private float _elapsed;
        private float _duration;
        private float _totalYaw;
        private float _appliedYaw;

        public bool IsActive { get; private set; }
        public Transform CurrentTarget => _heldTarget;

        /// <summary>같은 대상의 중복 요청은 강한 요청 하나로 합치며 진행 중 보정은 연장하지 않는다.</summary>
        public void Submit(in Request request)
        {
            if (request.Target == null || request.Strength <= 0f)
                return;

            for (int i = 0; i < _pendingCount; i++)
            {
                if (_pending[i].Target != request.Target)
                    continue;
                if (request.Strength > _pending[i].Strength)
                    _pending[i] = request;
                return;
            }

            if (_pendingCount < RequestCapacity)
                _pending[_pendingCount++] = request;
        }

        /// <summary>수동 조작·대상 교체·모드 전환 시 보정과 대기 요청만 제거한다.</summary>
        public void Reset()
        {
            ClearPending();
            IsActive = false;
            _active = default;
            _heldTarget = null;
            _holdRemaining = 0f;
            _intervalRemaining = 0f;
        }

        /// <summary>흔들림 이전 포즈를 기준으로 대상 선택과 유한한 회전 보정을 진행한다.</summary>
        public void Apply(ref CameraPose pose, CameraContext context, float deltaTime)
        {
            CameraUserPreferences preferences = CameraRuntimeServices.Adapter.UserPreferences;
            if (context?.Settings == null || context.Target == null || context.MainCamera == null
                || !preferences.HitAssistEnabled || preferences.AutoCorrectionScale <= 0f
                || context.Settings.combatCameraAutoCorrectionScale <= 0f
                || !CameraRuntimeServices.Adapter.IsGameplayInputActive
                || context.IsInputLocked || context.IsAligning || context.LookAtOverride != null
                || (context.LockOn?.IsActive ?? false)
                || (context.RotationTransition?.IsActive ?? false))
            {
                Reset();
                return;
            }

            float stepTime = Mathf.Max(0f, deltaTime);
            _intervalRemaining = Mathf.Max(0f, _intervalRemaining - stepTime);
            _holdRemaining = Mathf.Max(0f, _holdRemaining - stepTime);
            if (_holdRemaining <= 0f || !IsUsableTarget(_heldTarget, context.Target))
                _heldTarget = null;

            if (!IsActive && _intervalRemaining <= 0f)
                TryBegin(pose, context);
            ClearPending();

            if (IsActive)
                ApplyPulse(ref pose, context, stepTime);
        }

        private void TryBegin(in CameraPose pose, CameraContext context)
        {
            int selected = -1;
            float bestScore = float.PositiveInfinity;
            for (int i = 0; i < _pendingCount; i++)
            {
                Request request = _pending[i];
                if (_heldTarget != null && request.Target != _heldTarget)
                    continue;
                if (!TryEvaluate(request, pose, context, out Vector2 viewport, out _))
                    continue;

                float score = Mathf.Abs(viewport.x - 0.5f);
                if (score > bestScore || (score == bestScore && selected >= 0
                    && request.Target.GetInstanceID() >= _pending[selected].Target.GetInstanceID()))
                    continue;
                selected = i;
                bestScore = score;
            }

            if (selected < 0)
                return;

            _active = _pending[selected];
            _heldTarget = _active.Target;
            _holdRemaining = Mathf.Max(0f, context.Settings.hitAssistTargetHoldTime);
            _intervalRemaining = Mathf.Max(0f, context.Settings.hitAssistInterval);
            TryEvaluate(_active, pose, context, out _, out float correction);
            float limit = Mathf.Max(0f, context.Settings.hitAssistMaxYawPerPulse);
            _totalYaw = Mathf.Clamp(correction * _active.Strength, -limit, limit);
            float maxSpeed = Mathf.Max(0f, context.Settings.hitAssistMaxYawSpeed);
            if (Mathf.Abs(_totalYaw) <= 0.001f || maxSpeed <= 0f)
                return;

            // SmoothStep의 최대 기울기는 1.5이므로 구간 전체에서 각속도 상한을 지킨다.
            _duration = Mathf.Max(_active.Duration, 1.5f * Mathf.Abs(_totalYaw) / maxSpeed);
            _elapsed = 0f;
            _appliedYaw = 0f;
            IsActive = true;
        }

        private void ApplyPulse(ref CameraPose pose, CameraContext context, float deltaTime)
        {
            if (!TryEvaluate(_active, pose, context, out _, out float correction)
                || Mathf.Abs(correction) <= 0.001f || Mathf.Sign(correction) != Mathf.Sign(_totalYaw))
            {
                IsActive = false;
                return;
            }

            _elapsed += deltaTime;
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / _duration));
            float desiredStep = _totalYaw * blend - _appliedYaw;
            float stepLimit = Mathf.Min(Mathf.Abs(correction),
                Mathf.Max(0f, context.Settings.hitAssistMaxYawSpeed) * deltaTime);
            float step = Mathf.Clamp(desiredStep, -stepLimit, stepLimit);
            _appliedYaw += step;
            RotatePose(ref pose, pose.Yaw + step);
            if (_elapsed >= _duration)
                IsActive = false;
        }

        private static bool TryEvaluate(in Request request, in CameraPose pose, CameraContext context,
            out Vector2 viewport, out float correction)
        {
            viewport = default;
            correction = 0f;
            if (!IsUsableTarget(request.Target, context.Target)
                || Time.unscaledTime - context.LastManualInputTime < request.ManualInputDelay)
                return false;

            Vector3 focus = request.Target.TransformPoint(request.LocalFocus);
            Vector3 direction = request.Target.position - context.Target.position;
            if (direction.x * direction.x + direction.z * direction.z < 0.001f)
                return false;
            float heading = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.DeltaAngle(pose.Yaw, heading);
            if (Mathf.Abs(angle) > request.MaxAngle)
                return false;
            if (!CameraDeadZoneTracker.TryProject(pose, focus, context.MainCamera.aspect, out viewport)
                || viewport.y < 0f || viewport.y > 1f || viewport.x < 0f || viewport.x > 1f
                || IsOccluded(pose.CameraPosition, focus, request.Target, context))
                return false;

            Vector2 zone = context.Settings.hitAssistHorizontalZone;
            float min = Mathf.Clamp(zone.x, 0f, 0.49f);
            float max = Mathf.Clamp(zone.y, 0.51f, 1f);
            if (viewport.x >= min && viewport.x <= max)
                return true;

            float boundary = viewport.x < min ? min : max;
            // 피벗 주위의 실제 궤도를 투영한다. 제자리 회전 공식은 가까운 적에서 과보정될 수 있다.
            float low = 0f;
            float high = angle;
            CameraPose candidate = pose;
            RotatePose(ref candidate, pose.Yaw + high);
            bool startsLeft = viewport.x < min;
            if (!CameraDeadZoneTracker.TryProject(candidate, focus, context.MainCamera.aspect, out Vector2 end)
                || (startsLeft ? end.x <= viewport.x : end.x >= viewport.x))
                return true;

            for (int i = 0; i < 12; i++)
            {
                float middle = (low + high) * 0.5f;
                RotatePose(ref candidate, pose.Yaw + middle);
                CameraDeadZoneTracker.TryProject(candidate, focus, context.MainCamera.aspect, out Vector2 projected);
                bool remainsOutside = startsLeft ? projected.x < boundary : projected.x > boundary;
                if (remainsOutside) low = middle;
                else high = middle;
            }
            correction = low;
            return true;
        }

        private static bool IsUsableTarget(Transform target, Transform player)
        {
            if (target == null || target == player || !target.gameObject.activeInHierarchy)
                return false;
            return !CameraRuntimeServices.Adapter.TryResolveTarget(target, out CameraTargetInfo info)
                   || (info.IsAlive && info.IsHostileToPlayer);
        }

        private static bool IsOccluded(Vector3 origin, Vector3 focus, Transform target, CameraContext context)
        {
            Vector3 direction = focus - origin;
            if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, direction.magnitude,
                    context.CollisionLayers, QueryTriggerInteraction.Ignore))
                return false;
            Transform obstacle = hit.transform;
            return obstacle != target && !obstacle.IsChildOf(target)
                   && obstacle != context.Target && !obstacle.IsChildOf(context.Target);
        }

        private static void RotatePose(ref CameraPose pose, float yaw)
        {
            pose.Yaw = Mathf.DeltaAngle(0f, yaw);
            pose.CameraRotation = Quaternion.Euler(pose.Pitch, pose.Yaw, 0f);
            pose.CameraPosition = pose.PivotPosition + pose.CameraRotation * Vector3.back * pose.Distance;
        }

        private void ClearPending()
        {
            Array.Clear(_pending, 0, _pendingCount);
            _pendingCount = 0;
        }
    }
}
