using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// Collision 판정을 저작하는 이벤트들이 공유하는 데이터(스펙 7.1 2안).
    /// Motion Timeline의 <c>BeginCollisionEvent</c>와 궁극기 타임라인의 데미지 윈도우가 같은 필드를 사용한다.
    ///
    /// <c>BeginCollisionEvent</c>는 기존 에셋 호환을 위해 필드를 평면으로 유지하므로 이 타입을 품지 않는다.
    /// 신규 이벤트만 이 타입을 사용한다.
    /// </summary>
    [Serializable]
    public sealed class CollisionEventData
    {
        [Tooltip("AttackInfoBase.hitPhases의 인덱스.")]
        public int hitPhaseIndex;

        [Tooltip("판정 소스. 부착형 HitBox 그룹 또는 이벤트가 직접 소유한 명시적 범위.")]
        public CollisionSourceType collisionSource = CollisionSourceType.AttachedHitboxGroup;

        [Tooltip("CombatHitbox.groupId. 비어 있으면 HitPhaseData 또는 Default 그룹을 사용한다.")]
        public string hitboxGroupId;

        [Tooltip("함께 활성화할 추가 CombatHitbox.groupId 목록.")]
        public List<string> additionalHitboxGroupIds = new();

        [Tooltip("이벤트가 직접 소유하는 명시적 판정 범위.")]
        public ExplicitCollisionShapeData explicitShape = new();

        public CollisionRequest BuildRequest(
            LayerMask targetLayerMask,
            Vector3? overrideWorldPosition = null,
            Transform overrideAnchor = null,
            Quaternion? overrideWorldRotation = null)
            => collisionSource == CollisionSourceType.ExplicitShape
                ? CollisionRequest.Explicit(
                    hitPhaseIndex,
                    targetLayerMask,
                    explicitShape,
                    overrideWorldPosition,
                    overrideAnchor,
                    overrideWorldRotation)
                : CollisionRequest.Attached(
                    hitPhaseIndex,
                    targetLayerMask,
                    hitboxGroupId,
                    additionalHitboxGroupIds);

        public string Describe()
        {
            if (collisionSource == CollisionSourceType.ExplicitShape)
                return explicitShape != null ? explicitShape.Describe() : "Shape 미설정";

            string groupLabel = string.IsNullOrWhiteSpace(hitboxGroupId) ? "Phase Default" : hitboxGroupId;
            if (additionalHitboxGroupIds != null && additionalHitboxGroupIds.Count > 0)
                groupLabel += $"+{additionalHitboxGroupIds.Count}";
            return groupLabel;
        }
    }
}
