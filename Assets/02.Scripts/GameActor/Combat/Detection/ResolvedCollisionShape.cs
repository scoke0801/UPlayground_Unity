using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// Combat이 Anchor를 해석해 만드는 명시적 판정의 실행 시점 상태.
    /// 에셋의 <see cref="ExplicitCollisionShapeData"/>를 런타임 저장소로 쓰지 않기 위한 분리 타입이다.
    /// </summary>
    public struct ResolvedCollisionShape
    {
        public CollisionShapeType ShapeType;
        public CollisionEvaluationType Evaluation;
        public CollisionDirectionType Direction;
        public CollisionAnchorSampling Sampling;

        /// <summary>FollowDuringWindow에서 매 프레임 다시 읽는 Anchor. Snapshot/World에서는 null일 수 있다.</summary>
        public Transform Anchor;

        /// <summary>SnapshotOnBegin 또는 Anchor 소멸 시 사용할 고정 포즈.</summary>
        public Vector3 SnapshotPosition;
        public Quaternion SnapshotRotation;

        public Vector3 LocalOffset;
        public Quaternion LocalRotation;

        public float Radius;
        public Vector3 BoxSize;
        public float CapsuleHeight;

        public bool IsValid;

        /// <summary>Anchor 포즈(Follow면 현재, 아니면 스냅샷)를 반환한다.</summary>
        public void GetAnchorPose(out Vector3 position, out Quaternion rotation)
        {
            if (Sampling == CollisionAnchorSampling.FollowDuringWindow && Anchor != null)
            {
                position = Anchor.position;
                rotation = Anchor.rotation;
                return;
            }

            position = SnapshotPosition;
            rotation = SnapshotRotation;
        }

        /// <summary>현재 프레임의 월드 판정 형상을 만든다.</summary>
        public bool TryGetWorldShape(out CombatHitboxShape shape)
        {
            shape = default;
            if (!IsValid)
                return false;

            GetAnchorPose(out Vector3 anchorPosition, out Quaternion anchorRotation);
            Vector3 center = anchorPosition + anchorRotation * LocalOffset;
            Quaternion rotation = anchorRotation * LocalRotation;

            switch (ShapeType)
            {
                case CollisionShapeType.Sphere:
                    shape = CombatHitboxShape.Sphere(center, Radius);
                    return true;

                case CollisionShapeType.Box:
                    shape = CombatHitboxShape.Box(center, rotation, BoxSize * 0.5f);
                    return true;

                default:
                {
                    // Unity CapsuleCollider와 동일하게 height는 캡 포함 전체 높이로 해석한다.
                    float half = Mathf.Max(0f, CapsuleHeight * 0.5f - Radius);
                    Vector3 up = rotation * Vector3.up;
                    shape = CombatHitboxShape.Capsule(
                        center,
                        center - up * half,
                        center + up * half,
                        Radius);
                    return true;
                }
            }
        }

        /// <summary>Direction 정책에 따른 공격 방향. 길이가 0에 가까우면 ownerRoot.forward로 폴백한다.</summary>
        public Vector3 ResolveAttackDirection(Vector3 hitPoint, Vector3 shapeCenter, Transform ownerRoot)
        {
            GetAnchorPose(out _, out Quaternion anchorRotation);

            Vector3 direction = Direction switch
            {
                CollisionDirectionType.ShapeCenterToTarget => hitPoint - shapeCenter,
                CollisionDirectionType.TargetToShapeCenter => shapeCenter - hitPoint,
                CollisionDirectionType.ActorForward => ownerRoot != null ? ownerRoot.forward : Vector3.forward,
                _ => anchorRotation * Vector3.forward,
            };

            if (direction.sqrMagnitude < 0.0001f)
                direction = ownerRoot != null ? ownerRoot.forward : Vector3.forward;

            return direction.normalized;
        }
    }
}
