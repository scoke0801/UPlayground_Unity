using System;
using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// Collision Event가 사용하는 판정 소스.
    /// AttachedHitboxGroup = 0은 필수다. 신규 필드를 직렬화하지 않은 기존 에셋이
    /// C# 기본값 0으로 기존 부착형 경로를 그대로 유지하기 위함이다.
    /// </summary>
    public enum CollisionSourceType
    {
        [InspectorName("부착형 HitBox 그룹 (기존 방식)")] AttachedHitboxGroup = 0,
        [InspectorName("명시적 범위 (폭발·광역)")] ExplicitShape = 1,
    }

    public enum CollisionShapeType
    {
        [InspectorName("구 (Sphere)")] Sphere,
        [InspectorName("박스 (Box)")] Box,
        [InspectorName("캡슐 (Capsule)")] Capsule,
    }

    public enum CollisionAnchorType
    {
        [InspectorName("액터 루트")] ActorRoot,
        [InspectorName("공격 원점")] AttackOrigin,
        [InspectorName("주 대상")] PrimaryTarget,
        [InspectorName("월드 좌표")] WorldPosition,
    }

    public enum CollisionAnchorSampling
    {
        [InspectorName("시작 시 위치 고정")] SnapshotOnBegin,
        [InspectorName("윈도우 동안 따라가기")] FollowDuringWindow,
    }

    public enum CollisionEvaluationType
    {
        [InspectorName("지속 판정 (Window)")] Window,
        [InspectorName("시작 시 1회 (폭발)")] OnceOnBegin,
    }

    public enum CollisionDirectionType
    {
        [InspectorName("중심 → 피격자 (방사·넉백)")] ShapeCenterToTarget,
        [InspectorName("피격자 → 중심 (흡입·끌어당기기)")] TargetToShapeCenter,
        [InspectorName("액터 전방")] ActorForward,
        [InspectorName("Anchor 전방")] AnchorForward,
    }

    /// <summary>
    /// Collision Event가 직접 소유하는 명시적 판정 형상 저작 데이터.
    /// 런타임 상태(스냅샷된 Anchor 포즈 등)는 여기 쓰지 않고 <see cref="ResolvedCollisionShape"/>가 보유한다.
    /// </summary>
    [Serializable]
    public sealed class ExplicitCollisionShapeData
    {
        [Tooltip("판정 형상. Sphere/Box/Capsule만 지원한다.")]
        public CollisionShapeType shapeType = CollisionShapeType.Sphere;

        [Tooltip("판정 중심의 기준. PrimaryTarget/WorldPosition은 런타임 Context가 제공한다.")]
        public CollisionAnchorType anchor = CollisionAnchorType.ActorRoot;

        [Tooltip("SnapshotOnBegin은 시작 포즈를 고정하고, FollowDuringWindow는 Anchor를 계속 따라간다.")]
        public CollisionAnchorSampling anchorSampling = CollisionAnchorSampling.SnapshotOnBegin;

        [Tooltip("OnceOnBegin은 시작 시점에 정확히 한 번 판정한다. Window는 활성 구간 동안 프레임당 1회 판정한다.")]
        public CollisionEvaluationType evaluation = CollisionEvaluationType.OnceOnBegin;

        [Tooltip("피격자에게 전달할 공격 방향 정책.")]
        public CollisionDirectionType direction = CollisionDirectionType.ShapeCenterToTarget;

        [Tooltip("Anchor 로컬 기준 중심 오프셋.")]
        public Vector3 localOffset;

        [Tooltip("Anchor 로컬 기준 회전. Box/Capsule에만 의미가 있다.")]
        public Vector3 localEulerAngles;

        [Min(0.01f)] public float radius = 5f;
        public Vector3 boxSize = new(10f, 3f, 10f);
        [Min(0.01f)] public float capsuleHeight = 4f;

        [Tooltip("anchor가 WorldPosition일 때 사용하는 고정 월드 좌표.")]
        public Vector3 worldPosition;

        /// <summary>Shape 값 자체의 정합성 검사. 실패 시 <paramref name="error"/>에 사유를 담는다.</summary>
        public bool Validate(out string error)
        {
            switch (shapeType)
            {
                case CollisionShapeType.Sphere:
                    if (radius <= 0f)
                    {
                        error = $"Sphere radius가 0 이하입니다({radius}).";
                        return false;
                    }
                    break;

                case CollisionShapeType.Box:
                    if (boxSize.x <= 0f || boxSize.y <= 0f || boxSize.z <= 0f)
                    {
                        error = $"Box size의 모든 축이 0보다 커야 합니다({boxSize}).";
                        return false;
                    }
                    break;

                case CollisionShapeType.Capsule:
                    if (radius <= 0f)
                    {
                        error = $"Capsule radius가 0 이하입니다({radius}).";
                        return false;
                    }
                    if (capsuleHeight < radius * 2f)
                    {
                        error = $"Capsule height({capsuleHeight})는 radius * 2({radius * 2f}) 이상이어야 합니다.";
                        return false;
                    }
                    break;
            }

            error = null;
            return true;
        }

        /// <summary>Inspector·타임라인 라벨용 요약 문자열.</summary>
        public string Describe()
        {
            string shape = shapeType switch
            {
                CollisionShapeType.Sphere => $"Sphere {radius:0.##}m",
                CollisionShapeType.Box => $"Box {boxSize.x:0.##}×{boxSize.y:0.##}×{boxSize.z:0.##}",
                _ => $"Capsule r{radius:0.##} h{capsuleHeight:0.##}",
            };
            string anchorLabel = anchor switch
            {
                CollisionAnchorType.ActorRoot => "Actor",
                CollisionAnchorType.AttackOrigin => "Origin",
                CollisionAnchorType.PrimaryTarget => "Target",
                _ => "World",
            };
            string evalLabel = evaluation == CollisionEvaluationType.OnceOnBegin ? "Once" : "Window";
            return $"{shape} / {anchorLabel} / {evalLabel}";
        }
    }
}
