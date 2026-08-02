using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround;
using UPlayGround.Combat;
using UPlayGround.Data.Event;

namespace UPlayGround.Combat.Tests
{
    /// <summary>
    /// Collision Event 명시적 범위 판정(Explicit Shape)의 검출·방향·Anchor 계약 검증.
    /// 스펙: Assets/docs/Complete/COLLISION_EVENT_EXPLICIT_AREA_HIT_SPEC.md §11.1
    /// </summary>
    public sealed class ExplicitCollisionDetectionTests
    {
        private readonly List<GameObject> _spawned = new();
        private readonly Collider[] _buffer = new Collider[64];
        private readonly List<CombatHit> _results = new();
        private readonly HashSet<IDamageable> _collected = new();

        private const int TargetLayer = 0; // Default
        private static readonly LayerMask TargetMask = 1 << TargetLayer;

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            _spawned.Clear();
            _results.Clear();
            _collected.Clear();
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────

        private GameObject NewObject(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.layer = TargetLayer;
            _spawned.Add(go);
            return go;
        }

        /// <summary>피격 대상 하나. 콜라이더와 <see cref="StubDamageable"/>을 같은 GameObject에 둔다.</summary>
        private StubDamageable NewTarget(string name, Vector3 position, float radius = 0.5f)
        {
            GameObject go = NewObject(name, position);
            SphereCollider collider = go.AddComponent<SphereCollider>();
            collider.radius = radius;
            var damageable = go.AddComponent<StubDamageable>();
            Physics.SyncTransforms();
            return damageable;
        }

        private int Detect(in ResolvedCollisionShape shape, Transform ownerRoot = null)
        {
            _collected.Clear();
            return CombatHitDetector.DetectExplicitHits(
                ownerRoot,
                shape,
                TargetMask,
                _buffer,
                null,
                _collected,
                _results);
        }

        private static ResolvedCollisionShape Resolve(
            ExplicitCollisionShapeData data,
            Vector3 anchorPosition,
            Quaternion anchorRotation)
            => new()
            {
                ShapeType = data.shapeType,
                Evaluation = data.evaluation,
                Direction = data.direction,
                Sampling = CollisionAnchorSampling.SnapshotOnBegin,
                Anchor = null,
                SnapshotPosition = anchorPosition,
                SnapshotRotation = anchorRotation,
                LocalOffset = data.localOffset,
                LocalRotation = Quaternion.Euler(data.localEulerAngles),
                Radius = data.radius,
                BoxSize = data.boxSize,
                CapsuleHeight = data.capsuleHeight,
                IsValid = true,
            };

        // ── 기존 에셋 기본값 ─────────────────────────────────────────

        [Test]
        public void 신규_필드가_없는_기존_이벤트는_부착형_경로를_선택한다()
        {
            // CollisionSourceType.AttachedHitboxGroup이 0이어야 역직렬화 기본값이 종전 경로가 된다.
            Assert.AreEqual(0, (int)CollisionSourceType.AttachedHitboxGroup);

            var request = CollisionRequest.Attached(0, TargetMask, null, null);
            Assert.IsFalse(request.IsExplicit);
        }

        [Test]
        public void OnceOnBegin_명시적_이벤트는_애니메이션_평가_후_실행된다()
        {
            var collision = new BeginCollisionEvent
            {
                collisionSource = CollisionSourceType.ExplicitShape,
                explicitShape = new ExplicitCollisionShapeData
                {
                    evaluation = CollisionEvaluationType.OnceOnBegin,
                },
            };

            Assert.IsTrue(collision.RequiresPostEvaluation);

            collision.explicitShape.evaluation = CollisionEvaluationType.Window;
            Assert.IsFalse(collision.RequiresPostEvaluation);

            collision.collisionSource = CollisionSourceType.AttachedHitboxGroup;
            Assert.IsFalse(collision.RequiresPostEvaluation);
        }

        // ── Shape별 질의 ─────────────────────────────────────────────

        [Test]
        public void Sphere는_반경_내부_대상만_검출한다()
        {
            StubDamageable inside = NewTarget("Inside", new Vector3(2f, 0f, 0f));
            NewTarget("Outside", new Vector3(12f, 0f, 0f));

            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 5f,
            };

            int count = Detect(Resolve(data, Vector3.zero, Quaternion.identity));

            Assert.AreEqual(1, count);
            Assert.AreSame(inside, _results[0].Damageable);
        }

        [Test]
        public void 회전한_Box는_회전된_영역_내부_대상만_검출한다()
        {
            // Box 10(X) × 2(Y) × 2(Z)를 Y축 90도 회전 → 길쭉한 축이 Z를 향한다.
            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Box,
                boxSize = new Vector3(10f, 2f, 2f),
                localEulerAngles = new Vector3(0f, 90f, 0f),
            };

            StubDamageable alongZ = NewTarget("AlongZ", new Vector3(0f, 0f, 4f), 0.1f);
            NewTarget("AlongX", new Vector3(4f, 0f, 0f), 0.1f);

            int count = Detect(Resolve(data, Vector3.zero, Quaternion.identity));

            Assert.AreEqual(1, count, "회전 후 길쭉한 축(Z)에 있는 대상만 맞아야 한다.");
            Assert.AreSame(alongZ, _results[0].Damageable);
        }

        [Test]
        public void Capsule은_끝점과_원통_구간을_모두_검출한다()
        {
            // 높이 6, 반경 1 → 중심에서 위아래 각 2씩 실린더 + 캡.
            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Capsule,
                radius = 1f,
                capsuleHeight = 6f,
            };

            StubDamageable top = NewTarget("Top", new Vector3(0f, 2.8f, 0f), 0.1f);
            StubDamageable middle = NewTarget("Middle", new Vector3(0.5f, 0f, 0f), 0.1f);
            StubDamageable bottom = NewTarget("Bottom", new Vector3(0f, -2.8f, 0f), 0.1f);
            NewTarget("FarSide", new Vector3(5f, 0f, 0f), 0.1f);

            int count = Detect(Resolve(data, Vector3.zero, Quaternion.identity));

            Assert.AreEqual(3, count);
            CollectionAssert.Contains(DamageablesOf(_results), top);
            CollectionAssert.Contains(DamageablesOf(_results), middle);
            CollectionAssert.Contains(DamageablesOf(_results), bottom);
        }

        // ── 공통 수집 정책 ───────────────────────────────────────────

        [Test]
        public void 공격자와_하위_Collider는_결과에서_제외된다()
        {
            GameObject owner = NewObject("Owner", Vector3.zero);
            SphereCollider ownerCollider = owner.AddComponent<SphereCollider>();
            ownerCollider.radius = 0.5f;
            owner.AddComponent<StubDamageable>();

            GameObject childObject = NewObject("OwnerChild", new Vector3(0.5f, 0f, 0f));
            childObject.transform.SetParent(owner.transform, true);
            SphereCollider childCollider = childObject.AddComponent<SphereCollider>();
            childCollider.radius = 0.5f;
            childObject.AddComponent<StubDamageable>();

            StubDamageable other = NewTarget("Other", new Vector3(3f, 0f, 0f));
            Physics.SyncTransforms();

            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 8f,
            };

            int count = Detect(Resolve(data, Vector3.zero, Quaternion.identity), owner.transform);

            Assert.AreEqual(1, count);
            Assert.AreSame(other, _results[0].Damageable);
        }

        [Test]
        public void 다중_Collider_대상은_한_번만_반환된다()
        {
            GameObject go = NewObject("MultiCollider", new Vector3(2f, 0f, 0f));
            SphereCollider first = go.AddComponent<SphereCollider>();
            first.radius = 0.5f;
            SphereCollider second = go.AddComponent<SphereCollider>();
            second.radius = 0.8f;
            var damageable = go.AddComponent<StubDamageable>();
            Physics.SyncTransforms();

            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 6f,
            };

            int count = Detect(Resolve(data, Vector3.zero, Quaternion.identity));

            Assert.AreEqual(1, count);
            Assert.AreSame(damageable, _results[0].Damageable);
        }

        [Test]
        public void 이미_맞은_대상은_ignoredTargets로_제외된다()
        {
            StubDamageable already = NewTarget("Already", new Vector3(1f, 0f, 0f));
            StubDamageable fresh = NewTarget("Fresh", new Vector3(-1f, 0f, 0f));

            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 6f,
            };
            ResolvedCollisionShape shape = Resolve(data, Vector3.zero, Quaternion.identity);

            var ignored = new HashSet<IDamageable> { already };
            _collected.Clear();
            int count = CombatHitDetector.DetectExplicitHits(
                null,
                shape,
                TargetMask,
                _buffer,
                ignored,
                _collected,
                _results);

            Assert.AreEqual(1, count);
            Assert.AreSame(fresh, _results[0].Damageable);
        }

        [Test]
        public void 피격_불가_대상은_기본_검출에서_제외되고_무적_포함시에는_전달된다()
        {
            StubDamageable invincible = NewTarget("Invincible", new Vector3(2f, 0f, 0f));
            invincible.CanTakeDamageValue = false;

            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 6f,
            };
            ResolvedCollisionShape shape = Resolve(data, Vector3.zero, Quaternion.identity);

            Assert.AreEqual(0, Detect(shape), "무적 대상은 기본 경로에서 제외된다.");

            _collected.Clear();
            int count = CombatHitDetector.DetectExplicitHits(
                null,
                shape,
                TargetMask,
                _buffer,
                null,
                _collected,
                _results,
                includeInvincibleTargets: true);

            Assert.AreEqual(1, count, "방어/회피 Resolver까지 전달하려면 무적 대상도 검출돼야 한다.");
        }

        // ── 방향 정책 ────────────────────────────────────────────────

        [Test]
        public void 방사_방향은_Shape_중심에서_피격자를_향한다()
        {
            NewTarget("Target", new Vector3(3f, 0f, 0f), 0.2f);

            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 6f,
                direction = CollisionDirectionType.ShapeCenterToTarget,
            };

            Detect(Resolve(data, Vector3.zero, Quaternion.identity));

            Assert.AreEqual(1, _results.Count);
            Assert.Greater(Vector3.Dot(_results[0].AttackDirection, Vector3.right), 0.9f);
            Assert.AreEqual(1f, _results[0].AttackDirection.magnitude, 0.001f);
        }

        [Test]
        public void 흡입_방향은_피격자에서_Shape_중심을_향한다()
        {
            NewTarget("Target", new Vector3(3f, 0f, 0f), 0.2f);

            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 6f,
                direction = CollisionDirectionType.TargetToShapeCenter,
            };

            Detect(Resolve(data, Vector3.zero, Quaternion.identity));

            Assert.AreEqual(1, _results.Count);
            Assert.Less(Vector3.Dot(_results[0].AttackDirection, Vector3.right), -0.9f);
        }

        [Test]
        public void ActorForward_방향은_공격자_전방을_사용한다()
        {
            GameObject owner = NewObject("Owner", new Vector3(0f, 0f, -20f));
            owner.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // forward = +X

            NewTarget("Target", new Vector3(0f, 0f, 3f), 0.2f);

            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 6f,
                direction = CollisionDirectionType.ActorForward,
            };

            Detect(Resolve(data, Vector3.zero, Quaternion.identity), owner.transform);

            Assert.AreEqual(1, _results.Count);
            Assert.Greater(Vector3.Dot(_results[0].AttackDirection, Vector3.right), 0.99f);
        }

        [Test]
        public void AnchorForward_방향은_Anchor_회전을_사용한다()
        {
            NewTarget("Target", new Vector3(3f, 0f, 0f), 0.2f);

            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 6f,
                direction = CollisionDirectionType.AnchorForward,
            };

            // Anchor를 Y 90도 회전 → forward = +X
            Detect(Resolve(data, Vector3.zero, Quaternion.Euler(0f, 90f, 0f)));

            Assert.AreEqual(1, _results.Count);
            Assert.Greater(Vector3.Dot(_results[0].AttackDirection, Vector3.right), 0.99f);
        }

        // ── Anchor 샘플링 ────────────────────────────────────────────

        [Test]
        public void Snapshot_Anchor는_이동_후에도_시작_포즈를_유지한다()
        {
            GameObject anchor = NewObject("Anchor", Vector3.zero);

            var shape = new ResolvedCollisionShape
            {
                ShapeType = CollisionShapeType.Sphere,
                Sampling = CollisionAnchorSampling.SnapshotOnBegin,
                Anchor = anchor.transform,
                SnapshotPosition = anchor.transform.position,
                SnapshotRotation = anchor.transform.rotation,
                Radius = 3f,
                IsValid = true,
            };

            anchor.transform.position = new Vector3(50f, 0f, 0f);

            Assert.IsTrue(shape.TryGetWorldShape(out CombatHitboxShape world));
            Assert.AreEqual(Vector3.zero, world.Center);
        }

        [Test]
        public void Follow_Anchor는_이동하면_Shape도_따라간다()
        {
            GameObject anchor = NewObject("Anchor", Vector3.zero);

            var shape = new ResolvedCollisionShape
            {
                ShapeType = CollisionShapeType.Sphere,
                Sampling = CollisionAnchorSampling.FollowDuringWindow,
                Anchor = anchor.transform,
                SnapshotPosition = anchor.transform.position,
                SnapshotRotation = anchor.transform.rotation,
                Radius = 3f,
                IsValid = true,
            };

            anchor.transform.position = new Vector3(50f, 0f, 0f);

            Assert.IsTrue(shape.TryGetWorldShape(out CombatHitboxShape world));
            Assert.AreEqual(new Vector3(50f, 0f, 0f), world.Center);
        }

        [Test]
        public void localOffset은_Anchor_회전을_따라_적용된다()
        {
            var shape = new ResolvedCollisionShape
            {
                ShapeType = CollisionShapeType.Sphere,
                Sampling = CollisionAnchorSampling.SnapshotOnBegin,
                SnapshotPosition = Vector3.zero,
                SnapshotRotation = Quaternion.Euler(0f, 90f, 0f),
                LocalOffset = new Vector3(0f, 0f, 5f), // 로컬 전방 5m
                Radius = 1f,
                IsValid = true,
            };

            Assert.IsTrue(shape.TryGetWorldShape(out CombatHitboxShape world));
            Assert.AreEqual(5f, world.Center.x, 0.001f);
            Assert.AreEqual(0f, world.Center.z, 0.001f);
        }

        // ── Shape 값 검증 ────────────────────────────────────────────

        [Test]
        public void 잘못된_Shape_값은_검증_오류로_보고된다()
        {
            var zeroRadius = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 0f,
            };
            Assert.IsFalse(zeroRadius.Validate(out _));

            var badBox = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Box,
                boxSize = new Vector3(1f, 0f, 1f),
            };
            Assert.IsFalse(badBox.Validate(out _));

            var badCapsule = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Capsule,
                radius = 2f,
                capsuleHeight = 3f, // radius * 2 = 4 미만
            };
            Assert.IsFalse(badCapsule.Validate(out _));

            var valid = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Capsule,
                radius = 1f,
                capsuleHeight = 4f,
            };
            Assert.IsTrue(valid.Validate(out _));
        }

        // ── 세션 수명 ────────────────────────────────────────────────

        [Test]
        public void OnceOnBegin_세션은_1회_소비_후_다시_검출하지_않는다()
        {
            GameObject owner = NewObject("Owner", Vector3.zero);
            var anchors = owner.AddComponent<StubAnchorProvider>();

            var session = new CombatCollisionSession();
            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 3f,
                anchor = CollisionAnchorType.ActorRoot,
                evaluation = CollisionEvaluationType.OnceOnBegin,
            };

            Assert.IsTrue(session.TryBegin(data, anchors, null, out string error), error);
            Assert.IsTrue(session.ShouldDetect());

            session.MarkConsumed();
            Assert.IsFalse(session.ShouldDetect(), "OnceOnBegin은 소비 후 재질의하지 않는다.");

            session.End();
            Assert.IsFalse(session.IsActive);
        }

        [Test]
        public void Window_세션은_소비_표시와_무관하게_계속_검출한다()
        {
            GameObject owner = NewObject("Owner", Vector3.zero);
            var anchors = owner.AddComponent<StubAnchorProvider>();

            var session = new CombatCollisionSession();
            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 3f,
                evaluation = CollisionEvaluationType.Window,
            };

            Assert.IsTrue(session.TryBegin(data, anchors, null, out string error), error);
            session.MarkConsumed();
            Assert.IsTrue(session.ShouldDetect());

            session.End();
        }

        [Test]
        public void PrimaryTarget_Anchor는_대상이_없으면_조용히_폴백하지_않고_실패한다()
        {
            GameObject owner = NewObject("Owner", Vector3.zero);
            var anchors = owner.AddComponent<StubAnchorProvider>();
            anchors.PrimaryTarget = null;

            var session = new CombatCollisionSession();
            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 3f,
                anchor = CollisionAnchorType.PrimaryTarget,
            };

            Assert.IsFalse(session.TryBegin(data, anchors, null, out string error));
            Assert.IsNotNull(error);
            Assert.IsFalse(session.IsActive);
        }

        [Test]
        public void WorldPosition_Anchor는_런타임_Context_좌표를_우선한다()
        {
            GameObject owner = NewObject("Owner", Vector3.zero);
            var anchors = owner.AddComponent<StubAnchorProvider>();

            var session = new CombatCollisionSession();
            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 3f,
                anchor = CollisionAnchorType.WorldPosition,
                worldPosition = new Vector3(1f, 1f, 1f),
            };

            Assert.IsTrue(session.TryBegin(data, anchors, new Vector3(9f, 0f, 0f), out string error), error);
            Assert.IsTrue(session.Shape.TryGetWorldShape(out CombatHitboxShape world));
            Assert.AreEqual(new Vector3(9f, 0f, 0f), world.Center);

            session.End();
        }

        [Test]
        public void WorldPosition_Anchor는_런타임_Context_회전도_적용한다()
        {
            GameObject owner = NewObject("Owner", Vector3.zero);
            var anchors = owner.AddComponent<StubAnchorProvider>();

            var session = new CombatCollisionSession();
            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Box,
                boxSize = new Vector3(2f, 2f, 4f),
                anchor = CollisionAnchorType.WorldPosition,
                localOffset = Vector3.right,
                localEulerAngles = new Vector3(0f, 15f, 0f),
            };
            Quaternion stageRotation = Quaternion.Euler(0f, 90f, 0f);

            Assert.IsTrue(
                session.TryBegin(
                    data,
                    anchors,
                    new Vector3(9f, 0f, 0f),
                    null,
                    stageRotation,
                    out string error),
                error);
            Assert.IsTrue(session.Shape.TryGetWorldShape(out CombatHitboxShape world));
            Assert.That(
                Vector3.Distance(new Vector3(9f, 0f, 0f) + stageRotation * Vector3.right, world.Center),
                Is.LessThan(0.0001f));
            Assert.That(
                Quaternion.Angle(stageRotation * Quaternion.Euler(data.localEulerAngles), world.Rotation),
                Is.LessThan(0.01f));

            session.End();
        }

        [Test]
        public void PrimaryTarget_Anchor는_런타임_Context_지정_대상을_우선한다()
        {
            GameObject owner = NewObject("Owner", Vector3.zero);
            var anchors = owner.AddComponent<StubAnchorProvider>();
            GameObject providerTarget = NewObject("ProviderTarget", new Vector3(1f, 0f, 0f));
            anchors.PrimaryTarget = providerTarget.transform;

            GameObject contextTarget = NewObject("ContextTarget", new Vector3(20f, 0f, 0f));

            var session = new CombatCollisionSession();
            var data = new ExplicitCollisionShapeData
            {
                shapeType = CollisionShapeType.Sphere,
                radius = 3f,
                anchor = CollisionAnchorType.PrimaryTarget,
            };

            Assert.IsTrue(session.TryBegin(data, anchors, null, contextTarget.transform, out string error), error);
            Assert.IsTrue(session.Shape.TryGetWorldShape(out CombatHitboxShape world));
            Assert.AreEqual(new Vector3(20f, 0f, 0f), world.Center);

            session.End();
        }

        private static List<IDamageable> DamageablesOf(List<CombatHit> hits)
        {
            var list = new List<IDamageable>(hits.Count);
            foreach (CombatHit hit in hits)
                list.Add(hit.Damageable);
            return list;
        }

        // ── 스텁 ─────────────────────────────────────────────────────

        private sealed class StubDamageable : MonoBehaviour, IDamageable
        {
            public bool AliveValue = true;
            public bool CanTakeDamageValue = true;

            public CombatResult ReceiveHit(in HitRequest request) => default;
            public bool IsAlive() => AliveValue;
            public bool CanTakeDamage() => CanTakeDamageValue;
            public Transform GetTransform() => transform;
            public void LockOn() { }
            public void UnLockOn() { }
            public float GetHealthPercent() => 1f;
            public float GetCurrentHealth() => 1f;
            public void ApplyHealingEffect(float healAmount) { }
        }

        private sealed class StubAnchorProvider : MonoBehaviour, ICollisionAnchorProvider
        {
            public Transform AttackOrigin;
            public Transform PrimaryTarget;

            public Transform CollisionActorRoot => transform;
            public Transform CollisionAttackOrigin => AttackOrigin != null ? AttackOrigin : transform;
            public Transform CollisionPrimaryTarget => PrimaryTarget;
        }
    }
}
