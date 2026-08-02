using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// Collision 윈도우 시작에 필요한 모든 상태를 한 번에 전달하는 원자적 요청(스펙 5.1).
    ///
    /// setter를 순서대로 호출하던 기존 방식과 달리, 하나의 요청이 판정 소스·그룹·Shape를 모두
    /// 소유하므로 직전 윈도우의 그룹이나 Shape가 다음 윈도우에 잔존할 수 없다.
    /// </summary>
    public readonly struct CollisionRequest
    {
        public readonly int HitPhaseIndex;
        public readonly LayerMask TargetLayerMask;
        public readonly CollisionSourceType SourceType;

        /// <summary>AttachedHitboxGroup 전용 — 주 그룹. 비어 있으면 HitPhase 기본 그룹으로 폴백한다.</summary>
        public readonly string PrimaryHitboxGroupId;

        /// <summary>AttachedHitboxGroup 전용 — 함께 활성화할 추가 그룹.</summary>
        public readonly IReadOnlyList<string> AdditionalHitboxGroupIds;

        /// <summary>ExplicitShape 전용 — 이벤트가 소유한 저작 데이터.</summary>
        public readonly ExplicitCollisionShapeData ExplicitShape;

        /// <summary>ExplicitShape + WorldPosition Anchor에서 런타임 Context가 제공하는 좌표(궁극기 스테이지 등).</summary>
        public readonly Vector3? OverrideWorldPosition;

        /// <summary>ExplicitShape + WorldPosition Anchor에서 런타임 Context가 제공하는 회전.</summary>
        public readonly Quaternion? OverrideWorldRotation;

        /// <summary>
        /// ExplicitShape + PrimaryTarget Anchor에서 런타임 Context가 지정하는 대상.
        /// 궁극기처럼 Combat의 현재 공격 대상과 다른 주 대상을 가진 경로가 사용한다.
        /// </summary>
        public readonly Transform OverrideAnchor;

        public bool IsExplicit => SourceType == CollisionSourceType.ExplicitShape;

        private CollisionRequest(
            int hitPhaseIndex,
            LayerMask targetLayerMask,
            CollisionSourceType sourceType,
            string primaryHitboxGroupId,
            IReadOnlyList<string> additionalHitboxGroupIds,
            ExplicitCollisionShapeData explicitShape,
            Vector3? overrideWorldPosition,
            Quaternion? overrideWorldRotation,
            Transform overrideAnchor)
        {
            HitPhaseIndex = hitPhaseIndex;
            TargetLayerMask = targetLayerMask;
            SourceType = sourceType;
            PrimaryHitboxGroupId = primaryHitboxGroupId;
            AdditionalHitboxGroupIds = additionalHitboxGroupIds;
            ExplicitShape = explicitShape;
            OverrideWorldPosition = overrideWorldPosition;
            OverrideWorldRotation = overrideWorldRotation;
            OverrideAnchor = overrideAnchor;
        }

        public static CollisionRequest Attached(
            int hitPhaseIndex,
            LayerMask targetLayerMask,
            string primaryHitboxGroupId,
            IReadOnlyList<string> additionalHitboxGroupIds)
            => new(
                hitPhaseIndex,
                targetLayerMask,
                CollisionSourceType.AttachedHitboxGroup,
                primaryHitboxGroupId,
                additionalHitboxGroupIds,
                null,
                null,
                null,
                null);

        public static CollisionRequest Explicit(
            int hitPhaseIndex,
            LayerMask targetLayerMask,
            ExplicitCollisionShapeData explicitShape,
            Vector3? overrideWorldPosition = null,
            Transform overrideAnchor = null,
            Quaternion? overrideWorldRotation = null)
            => new(
                hitPhaseIndex,
                targetLayerMask,
                CollisionSourceType.ExplicitShape,
                null,
                null,
                explicitShape,
                overrideWorldPosition,
                overrideWorldRotation,
                overrideAnchor);
    }
}
