using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (800) 카메라 위치 충돌 보정 체인:
    /// CameraCollision.Evaluate → SafeBackPosition → GroundPenetration → FloorRescue → 캐릭터 캡슐 내부 진입 방지.
    /// 피벗(스무딩)은 frame.Pose.PivotPosition, SafeBack 원점(비스무딩)은 frame.PivotBase에서 읽는다.
    /// camDir/거리는 State+Effects에서 재계산하여 다른 Modifier에 직접 의존하지 않는다.
    /// 원본: InGameCameraMode.EvaluateCameraPosition(충돌부) + Resolve* 헬퍼들 + ComputeCapsuleClearance
    /// </summary>
    public sealed class CollisionCameraModifier : ICameraModifier
    {
        private float _safeBackDistance = -1f;
        private float _safeBackDistanceVel;

        public int Priority => 800;

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null || frame.State == null) return;

            CameraSettings settings = context.Settings;
            CameraState state = frame.State;
            float deltaTime = frame.DeltaTime;

            Vector3 pivotPosition = frame.Pose.PivotPosition;
            Vector3 pivotBase = frame.PivotBase;

            Quaternion targetRot = Quaternion.Euler(state.CurrentPitch, state.CurrentYaw, 0f);
            Vector3 camDir = targetRot * Vector3.back;
            float desiredDistance = Mathf.Clamp(state.TargetDistance, settings.minDistance, settings.maxDistance)
                                    + frame.Effects.distanceDelta;

            float finalDist = context.Collision != null
                ? context.Collision.Evaluate(pivotPosition, camDir, desiredDistance)
                : desiredDistance;

            Vector3 backPos = pivotPosition + camDir * finalDist;
            backPos = ResolveSafeBackPosition(context, settings, pivotBase, backPos, deltaTime);
            backPos = ResolveGroundPenetration(context, settings, pivotPosition, camDir, backPos);
            context.Collision?.ApplyFloorRescue(pivotPosition, ref backPos, deltaTime);
            backPos = ResolveCharacterCapsuleExclusion(context, settings, pivotPosition, camDir, backPos);

            frame.Pose.CameraPosition = backPos;
        }

        private Vector3 ResolveSafeBackPosition(
            CameraContext context,
            CameraSettings settings,
            Vector3 pivotBase,
            Vector3 backPos,
            float deltaTime)
        {
            Vector3 toCam = backPos - pivotBase;
            float toCamDist = toCam.magnitude;
            if (toCamDist <= 0.01f) return backPos;

            Vector3 toCamDir = toCam / toCamDist;
            if (_safeBackDistance < 0f)
                _safeBackDistance = toCamDist;

            float targetDistance = toCamDist;
            if (Physics.SphereCast(pivotBase, settings.cameraRadius, toCamDir,
                    out RaycastHit safeHit, toCamDist, context.CollisionLayers))
            {
                if (safeHit.transform != context.Target && !safeHit.transform.IsChildOf(context.Target))
                {
                    targetDistance = Mathf.Max(safeHit.distance - settings.collisionOffset, 0f);
                }
            }

            // Pass 2는 클리핑 방지용 백스톱이다(아래 Min 클램프로 절대 더 밀어내지 않음).
            // 복귀(밖으로)는 Pass 1(CameraCollision.Evaluate)이 이미 collisionReturnSpeed로 스무딩하므로,
            // 여기서 또 스무딩하면 2단 러버밴딩이 생겨 벽에서 떨어질 때 굼뜬 복귀가 된다 → 복귀는 즉시 추종.
            // 안으로 당기는(클리핑 회피) 경우만 빠르게 스무딩해 안전성을 유지한다.
            if (targetDistance >= _safeBackDistance)
            {
                _safeBackDistance = targetDistance;
                _safeBackDistanceVel = 0f;
            }
            else
            {
                float smoothTime = settings.collisionOccludedSmoothTime;
                float maxSpeed = settings.collisionMaxDistanceChangeSpeed > 0f
                    ? settings.collisionMaxDistanceChangeSpeed
                    : Mathf.Infinity;

                _safeBackDistance = smoothTime > 0f
                    ? Mathf.SmoothDamp(
                        _safeBackDistance,
                        targetDistance,
                        ref _safeBackDistanceVel,
                        smoothTime,
                        maxSpeed,
                        Mathf.Max(deltaTime, 0.0001f))
                    : Mathf.MoveTowards(_safeBackDistance, targetDistance, maxSpeed * Mathf.Max(deltaTime, 0.0001f));
            }

            _safeBackDistance = Mathf.Min(_safeBackDistance, toCamDist);
            return pivotBase + toCamDir * _safeBackDistance;
        }

        private static Vector3 ResolveGroundPenetration(
            CameraContext context,
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

        private static Vector3 ResolveCharacterCapsuleExclusion(
            CameraContext context,
            CameraSettings settings,
            Vector3 pivotPosition,
            Vector3 camDir,
            Vector3 backPos)
        {
            if (Time.frameCount <= context.SuppressCapsuleClearanceUntilFrame)
                return backPos;

            ComputeCapsuleClearance(context.CharacterCapsule, settings, pivotPosition, camDir,
                out float backClearance, out _);

            float backDist = Vector3.Distance(pivotPosition, backPos);
            float minBackDistance = backClearance + 0.03f;
            if (backDist >= minBackDistance)
                return backPos;

            return pivotPosition + camDir * minBackDistance;
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
