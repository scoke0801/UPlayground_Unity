using System;
using UnityEditor;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// Collision 이벤트의 명시적 판정 범위를 MotionSet 프리뷰 캐릭터 기준으로 Scene View에 표시한다.
    /// 실제 런타임이 질의하는 것과 동일한 <see cref="ResolvedCollisionShape"/> 계산을 사용해
    /// 표시 형상과 Physics 질의 형상이 어긋나지 않게 한다.
    /// </summary>
    public sealed class BeginCollisionEventSceneEditor : IMotionEventSceneEditor
    {
        private static readonly Color SphereColor = new(0.3f, 0.7f, 1f, 0.9f);
        private static readonly Color BoxColor = new(1f, 0.55f, 0.2f, 0.9f);
        private static readonly Color CapsuleColor = new(0.5f, 1f, 0.4f, 0.9f);
        private static readonly Color AnchorColor = new(1f, 0.85f, 0.2f, 0.9f);

        public Type EventType => typeof(BeginCollisionEvent);

        public void OnInspectorGUI(MotionEventBase motionEvent, IMotionEditorContext context)
        {
            if (motionEvent is not BeginCollisionEvent collision)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Collision 판정 검증", EditorStyles.boldLabel);

                if (collision.collisionSource == CollisionSourceType.AttachedHitboxGroup)
                {
                    bool noGroup = string.IsNullOrWhiteSpace(collision.hitboxGroupId)
                                   && (collision.additionalHitboxGroupIds == null
                                       || collision.additionalHitboxGroupIds.Count == 0);
                    EditorGUILayout.HelpBox(
                        noGroup
                            ? "그룹이 비어 있어 HitPhaseData의 기본 HitBox 그룹을 사용합니다."
                            : $"부착형 HitBox 그룹: {collision.GetShortLabel()}",
                        MessageType.Info);
                    return;
                }

                if (collision.explicitShape == null)
                {
                    EditorGUILayout.HelpBox(
                        "판정 소스가 Explicit Shape인데 Shape 데이터가 없습니다.",
                        MessageType.Error);
                    return;
                }

                if (!collision.explicitShape.Validate(out string error))
                    EditorGUILayout.HelpBox(error, MessageType.Error);

                if (collision.explicitShape.evaluation == CollisionEvaluationType.OnceOnBegin)
                {
                    EditorGUILayout.HelpBox(
                        "OnceOnBegin은 시작 시점에 정확히 1회 판정합니다. duration은 판정에 영향을 주지 않으며 " +
                        "타임라인 블록 표시용으로만 유지됩니다.",
                        MessageType.None);
                }
                else if (collision.endTime - collision.startTime <= 0f)
                {
                    EditorGUILayout.HelpBox(
                        "Window 평가인데 duration이 0입니다. 판정 기회가 없을 수 있으니 " +
                        "duration을 주거나 OnceOnBegin으로 전환하세요.",
                        MessageType.Error);
                }

                if (collision.explicitShape.anchor == CollisionAnchorType.PrimaryTarget)
                {
                    EditorGUILayout.HelpBox(
                        "PrimaryTarget Anchor는 런타임 주 대상이 필요합니다. Scene 프리뷰는 " +
                        "대상이 없으면 표시되지 않으며, 런타임에 대상이 없으면 판정이 중단됩니다.",
                        MessageType.Warning);
                }

                if (collision.explicitShape.anchorSampling == CollisionAnchorSampling.FollowDuringWindow
                    && collision.explicitShape.shapeType != CollisionShapeType.Sphere)
                {
                    EditorGUILayout.HelpBox(
                        "회전 가능한 Box/Capsule + Anchor Follow 조합은 프리뷰가 시작 포즈만 보여줍니다. " +
                        "실제 판정은 매 프레임 Anchor 회전을 따라갑니다.",
                        MessageType.Warning);
                }

                EditorGUILayout.LabelField("요약", collision.explicitShape.Describe());
            }
        }

        public bool OnSceneGUI(MotionEventBase motionEvent, IMotionEditorContext context)
        {
            if (motionEvent is not BeginCollisionEvent collision)
                return false;
            if (collision.collisionSource != CollisionSourceType.ExplicitShape)
                return false;
            if (collision.explicitShape == null || !collision.explicitShape.Validate(out _))
                return false;

            GameObject root = context?.Subject?.Root;
            if (root == null)
                return false;

            if (!TryResolvePreviewShape(collision.explicitShape, root.transform, out ResolvedCollisionShape resolved))
                return false;
            if (!resolved.TryGetWorldShape(out CombatHitboxShape shape))
                return false;

            Color color = collision.explicitShape.shapeType switch
            {
                CollisionShapeType.Sphere => SphereColor,
                CollisionShapeType.Box => BoxColor,
                _ => CapsuleColor,
            };

            using (new Handles.DrawingScope(color))
                DrawShape(shape);

            resolved.GetAnchorPose(out Vector3 anchorPosition, out _);
            using (new Handles.DrawingScope(AnchorColor))
            {
                Handles.DrawDottedLine(anchorPosition, shape.Center, 3f);
                Handles.SphereHandleCap(0, anchorPosition, Quaternion.identity, 0.2f, EventType_Repaint);
                DrawDirectionArrow(collision.explicitShape, resolved, shape, root.transform);
            }

            // 프리뷰는 읽기 전용이다. 에셋을 더럽히지 않는다.
            return false;
        }

        private const UnityEngine.EventType EventType_Repaint = UnityEngine.EventType.Repaint;

        /// <summary>
        /// 프리뷰용 Anchor 해석. 런타임 Combat이 없는 에디터에서는 프리뷰 루트를 Actor Root로 쓰고,
        /// AttackOrigin은 루트로 근사한다. PrimaryTarget은 프리뷰 대상이 없으므로 표시하지 않는다.
        /// </summary>
        private static bool TryResolvePreviewShape(
            ExplicitCollisionShapeData data,
            Transform previewRoot,
            out ResolvedCollisionShape resolved)
        {
            resolved = default;

            Transform anchor = data.anchor switch
            {
                CollisionAnchorType.ActorRoot => previewRoot,
                CollisionAnchorType.AttackOrigin => previewRoot,
                CollisionAnchorType.PrimaryTarget => null,
                _ => null,
            };

            if (data.anchor == CollisionAnchorType.PrimaryTarget)
                return false;

            Vector3 anchorPosition = anchor != null ? anchor.position : data.worldPosition;
            Quaternion anchorRotation = anchor != null ? anchor.rotation : Quaternion.identity;

            resolved = new ResolvedCollisionShape
            {
                ShapeType = data.shapeType,
                Evaluation = data.evaluation,
                Direction = data.direction,
                Sampling = CollisionAnchorSampling.SnapshotOnBegin,
                Anchor = anchor,
                SnapshotPosition = anchorPosition,
                SnapshotRotation = anchorRotation,
                LocalOffset = data.localOffset,
                LocalRotation = Quaternion.Euler(data.localEulerAngles),
                Radius = data.radius,
                BoxSize = data.boxSize,
                CapsuleHeight = data.capsuleHeight,
                IsValid = true,
            };
            return true;
        }

        private static void DrawShape(in CombatHitboxShape shape)
        {
            switch (shape.Type)
            {
                case CombatHitboxShapeType.Sphere:
                    DrawWireSphere(shape.Center, shape.Radius);
                    break;

                case CombatHitboxShapeType.Box:
                {
                    Matrix4x4 previous = Handles.matrix;
                    Handles.matrix = Matrix4x4.TRS(shape.Center, shape.Rotation, Vector3.one);
                    Handles.DrawWireCube(Vector3.zero, shape.HalfExtents * 2f);
                    Handles.matrix = previous;
                    break;
                }

                default:
                    DrawWireSphere(shape.Point0, shape.Radius);
                    DrawWireSphere(shape.Point1, shape.Radius);
                    Vector3 axis = shape.Point1 - shape.Point0;
                    Vector3 radial = axis.sqrMagnitude > 0.0001f
                        ? Vector3.Cross(axis, Vector3.up).normalized
                        : Vector3.right;
                    if (radial.sqrMagnitude < 0.0001f)
                        radial = Vector3.right;
                    Handles.DrawLine(shape.Point0 + radial * shape.Radius, shape.Point1 + radial * shape.Radius);
                    Handles.DrawLine(shape.Point0 - radial * shape.Radius, shape.Point1 - radial * shape.Radius);
                    break;
            }
        }

        private static void DrawWireSphere(Vector3 center, float radius)
        {
            Handles.DrawWireDisc(center, Vector3.up, radius);
            Handles.DrawWireDisc(center, Vector3.right, radius);
            Handles.DrawWireDisc(center, Vector3.forward, radius);
        }

        private static void DrawDirectionArrow(
            ExplicitCollisionShapeData data,
            in ResolvedCollisionShape resolved,
            in CombatHitboxShape shape,
            Transform ownerRoot)
        {
            float length = Mathf.Max(0.6f, shape.Radius > 0f ? shape.Radius : shape.HalfExtents.magnitude);
            // 방사/흡입은 중심 기준 의미를 보여주기 위해 가상의 피격점을 하나 잡아 화살표를 그린다.
            Vector3 sampleHitPoint = shape.Center + ownerRoot.forward * length;
            Vector3 direction = resolved.ResolveAttackDirection(sampleHitPoint, shape.Center, ownerRoot);
            Vector3 origin = data.direction == CollisionDirectionType.TargetToShapeCenter
                ? sampleHitPoint
                : shape.Center;
            Handles.ArrowHandleCap(
                0,
                origin,
                Quaternion.LookRotation(direction),
                length,
                EventType_Repaint);
        }
    }
}
