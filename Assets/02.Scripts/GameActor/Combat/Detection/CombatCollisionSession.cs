using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// Combat이 Collision Event의 Anchor를 해석할 때 제공하는 기준 Transform 모음.
    /// PlayerCombat / EnemyCombat / ResidualPlayerCombat이 구현한다.
    /// </summary>
    public interface ICollisionAnchorProvider
    {
        Transform CollisionActorRoot { get; }
        Transform CollisionAttackOrigin { get; }
        Transform CollisionPrimaryTarget { get; }
    }

    /// <summary>
    /// 명시적 범위 판정(Explicit Shape) 윈도우의 런타임 상태 소유자.
    ///
    /// <see cref="CombatHitboxSet"/>은 이름과 책임상 부착형 HitBox 그룹 저장소이므로,
    /// 요청·평가 방식·해석된 Shape·Anchor 스냅샷은 이 세션이 별도로 소유한다(스펙 5.4 A안).
    /// MonoBehaviour가 아니라 각 Combat이 필드로 보유한다.
    /// </summary>
    public sealed class CombatCollisionSession
    {
        private readonly Collider[] _overlapBuffer = new Collider[128];
        private readonly HashSet<IDamageable> _frameDamageables = new();

        private ResolvedCollisionShape _shape;

        public bool IsActive { get; private set; }

        public CollisionEvaluationType Evaluation => _shape.Evaluation;

        /// <summary>OnceOnBegin 판정이 이미 소비됐는지. Window 평가에서는 항상 false.</summary>
        public bool IsConsumed { get; private set; }

        public ResolvedCollisionShape Shape => _shape;

        /// <summary>
        /// 저작 데이터와 Anchor 제공자로부터 실행 시점 Shape를 만든다.
        /// Anchor 해석 실패는 조용히 ActorRoot로 폴백하지 않고 실패로 보고한다(스펙 5.5).
        /// </summary>
        public bool TryBegin(
            ExplicitCollisionShapeData data,
            ICollisionAnchorProvider anchors,
            Vector3? overrideWorldPosition,
            out string error)
            => TryBegin(data, anchors, overrideWorldPosition, null, out error);

        /// <inheritdoc cref="TryBegin(ExplicitCollisionShapeData, ICollisionAnchorProvider, Vector3?, out string)"/>
        /// <param name="overrideAnchor">
        /// PrimaryTarget Anchor를 런타임 Context가 직접 지정할 때 사용한다(궁극기 등).
        /// 지정되면 <paramref name="anchors"/>의 주 대상보다 우선한다.
        /// </param>
        public bool TryBegin(
            ExplicitCollisionShapeData data,
            ICollisionAnchorProvider anchors,
            Vector3? overrideWorldPosition,
            Transform overrideAnchor,
            out string error)
            => TryBegin(data, anchors, overrideWorldPosition, overrideAnchor, null, out error);

        /// <summary>런타임 Context가 WorldPosition Anchor의 위치와 회전을 함께 지정하는 진입점.</summary>
        public bool TryBegin(
            ExplicitCollisionShapeData data,
            ICollisionAnchorProvider anchors,
            Vector3? overrideWorldPosition,
            Transform overrideAnchor,
            Quaternion? overrideWorldRotation,
            out string error)
        {
            End();

            if (data == null)
            {
                error = "Explicit Shape 데이터가 null입니다.";
                return false;
            }

            if (!data.Validate(out error))
                return false;

            Transform anchorTransform = null;
            Vector3 anchorPosition;
            Quaternion anchorRotation;

            switch (data.anchor)
            {
                case CollisionAnchorType.ActorRoot:
                    anchorTransform = anchors?.CollisionActorRoot;
                    if (anchorTransform == null)
                    {
                        error = "Anchor=ActorRoot이지만 Actor Root Transform을 찾지 못했습니다.";
                        return false;
                    }
                    break;

                case CollisionAnchorType.AttackOrigin:
                    anchorTransform = anchors?.CollisionAttackOrigin;
                    if (anchorTransform == null)
                    {
                        error = "Anchor=AttackOrigin이지만 공격 원점 Transform이 설정되지 않았습니다.";
                        return false;
                    }
                    break;

                case CollisionAnchorType.PrimaryTarget:
                    anchorTransform = overrideAnchor != null ? overrideAnchor : anchors?.CollisionPrimaryTarget;
                    if (anchorTransform == null)
                    {
                        error = "Anchor=PrimaryTarget이지만 현재 액션에 주 대상이 없습니다.";
                        return false;
                    }
                    break;

                case CollisionAnchorType.WorldPosition:
                    break;
            }

            if (anchorTransform != null)
            {
                anchorPosition = anchorTransform.position;
                anchorRotation = anchorTransform.rotation;
            }
            else
            {
                // 런타임 Context(궁극기 스테이지 등)가 좌표를 제공하면 그것이 우선한다.
                anchorPosition = overrideWorldPosition ?? data.worldPosition;
                anchorRotation = overrideWorldRotation ?? Quaternion.identity;
            }

            _shape = new ResolvedCollisionShape
            {
                ShapeType = data.shapeType,
                Evaluation = data.evaluation,
                Direction = data.direction,
                Sampling = data.anchorSampling,
                Anchor = anchorTransform,
                SnapshotPosition = anchorPosition,
                SnapshotRotation = anchorRotation,
                LocalOffset = data.localOffset,
                LocalRotation = Quaternion.Euler(data.localEulerAngles),
                Radius = data.radius,
                BoxSize = data.boxSize,
                CapsuleHeight = data.capsuleHeight,
                IsValid = true,
            };

            IsActive = true;
            IsConsumed = false;
            error = null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ExplicitCollisionDebugRegistry.Register(this);
#endif
            return true;
        }

        public void End()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IsActive)
                ExplicitCollisionDebugRegistry.Unregister(this);
#endif
            IsActive = false;
            IsConsumed = false;
            _shape = default;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LastQueriedShape = default;
            HasLastQueriedShape = false;
#endif
        }

        /// <summary>OnceOnBegin 판정을 1회 소비 처리한다.</summary>
        public void MarkConsumed() => IsConsumed = true;

        /// <summary>이 세션이 이번 프레임 검출을 수행해야 하는지.</summary>
        public bool ShouldDetect()
        {
            if (!IsActive || !_shape.IsValid)
                return false;
            return _shape.Evaluation != CollisionEvaluationType.OnceOnBegin || !IsConsumed;
        }

        public int Detect(
            Transform ownerRoot,
            LayerMask targetLayer,
            ISet<IDamageable> ignoredTargets,
            List<CombatHit> results,
            bool includeInvincibleTargets)
        {
            _frameDamageables.Clear();
            int count = CombatHitDetector.DetectExplicitHits(
                ownerRoot,
                _shape,
                targetLayer,
                _overlapBuffer,
                ignoredTargets,
                _frameDamageables,
                results,
                includeInvincibleTargets);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_shape.TryGetWorldShape(out CombatHitboxShape queried))
            {
                LastQueriedShape = queried;
                HasLastQueriedShape = true;
            }
#endif
            return count;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>디버그 표시용 — 마지막으로 실제 Physics 질의에 사용한 형상.</summary>
        public CombatHitboxShape LastQueriedShape { get; private set; }
        public bool HasLastQueriedShape { get; private set; }
#endif
    }
}
