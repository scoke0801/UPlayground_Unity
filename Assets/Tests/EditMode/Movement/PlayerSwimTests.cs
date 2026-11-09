using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.State;

namespace UPlayGround.Movement.Tests
{
    /// <summary>수영 진입·이탈 기준, 부력 계산, 수면 높이 조회, 모션 태그 등록을 확인한다.</summary>
    public sealed class PlayerSwimTests
    {
        [TestCase(1.19f, false)]
        [TestCase(1.2f, true)]
        [TestCase(2f, true)]
        public void ShouldEnter_진입깊이_이상일때만_true(float depth, bool expected) =>
            Assert.That(PlayerSwimRules.ShouldEnter(depth, 1.2f), Is.EqualTo(expected));

        [TestCase(1.0f, true, false)]  // 진입~이탈 사이는 유지(깜박임 방지)
        [TestCase(0.8f, true, true)]
        [TestCase(0.8f, false, false)] // 디딜 곳 없이 얕아져도 이탈하지 않음
        [TestCase(-0.1f, false, true)] // 완전히 물 밖
        public void ShouldExit_디딜곳이_있거나_물밖일때만_true(float depth, bool hasGround, bool expected) =>
            Assert.That(PlayerSwimRules.ShouldExit(depth, 0.85f, hasGround), Is.EqualTo(expected));

        [Test]
        public void 진입깊이는_이탈깊이보다_깊어야_경계에서_깜박이지_않는다()
        {
            var go = new GameObject("swim defaults");
            go.SetActive(false); // Awake가 모터 부재 오류를 내지 않도록 비활성 상태로 붙인다.
            try
            {
                var controller = go.AddComponent<UPlayGround.MovementController.PlayerMovementController>();
                Assert.That(controller.SwimEnterDepth, Is.GreaterThan(controller.SwimExitDepth));
                Assert.That(controller.SwimFloatDepth, Is.GreaterThan(controller.SwimExitDepth));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ResolveVerticalSpeed_목표로_향하고_최대속도로_제한한다()
        {
            Assert.That(PlayerSwimRules.ResolveVerticalSpeed(0f, 0.5f, 6f, 6f), Is.EqualTo(3f).Within(0.0001f));
            Assert.That(PlayerSwimRules.ResolveVerticalSpeed(0f, 5f, 6f, 6f), Is.EqualTo(6f));
            Assert.That(PlayerSwimRules.ResolveVerticalSpeed(0f, -5f, 6f, 6f), Is.EqualTo(-6f));
            Assert.That(PlayerSwimRules.ResolveVerticalSpeed(1f, 1f, 6f, 6f), Is.Zero);
        }

        [Test]
        public void ResolveHopSpeed_정점이_목표높이에_닿는다()
        {
            float speed = PlayerSwimRules.ResolveHopSpeed(1.2f, 45f);
            Assert.That(speed * speed / (2f * 45f), Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(PlayerSwimRules.ResolveHopSpeed(-1f, 45f), Is.Zero);
        }

        [TestCase(1.7f, false)]   // 머리 높이와 같으면 아직 잠기지 않음
        [TestCase(1.71f, true)]
        [TestCase(1.35f, false)]  // 수면에 떠 있는 깊이
        public void IsSubmerged_수심이_머리높이를_넘을때만_true(float depth, bool expected) =>
            Assert.That(PlayerSwimRules.IsSubmerged(depth, 1.7f), Is.EqualTo(expected));

        [TestCase(-5.1f, -2f, 0f)]   // 최대 깊이 아래로는 더 내려가지 않는다
        [TestCase(-3f, -2f, -2f)]    // 수면 아래에서 하강 요청은 그대로
        [TestCase(-3f, 0f, 0f)]      // 입력이 없으면 깊이 유지
        [TestCase(-3f, 2.5f, 2.5f)]  // 상승 요청은 수면에서 멀면 그대로
        public void ResolveDiveVerticalSpeed_수면아래에서는_요청을_따르고_최대깊이에서_멈춘다(float feetY, float command, float expected)
        {
            // 수면 y=0 기준: 부력 높이 -1.35, 최대 깊이 -5.
            float actual = PlayerSwimRules.ResolveDiveVerticalSpeed(feetY, -1.35f, -5f, command, 6f, 6f);
            Assert.That(actual, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void ResolveDiveVerticalSpeed_수면높이에서는_하강만_받고_나머지는_부력이_띄운다()
        {
            const float floatY = -1.35f;
            Assert.That(PlayerSwimRules.ResolveDiveVerticalSpeed(floatY, floatY, -5f, -2f, 6f, 6f), Is.EqualTo(-2f));
            Assert.That(PlayerSwimRules.ResolveDiveVerticalSpeed(floatY - 0.5f, floatY, -5f, 0f, 6f, 6f), Is.EqualTo(0f));
            // 수면 위로 솟구쳐 있으면 스프링이 끌어내린다.
            Assert.That(PlayerSwimRules.ResolveDiveVerticalSpeed(floatY + 0.3f, floatY, -5f, 3f, 6f, 6f), Is.LessThan(0f));
        }

        [Test]
        public void ResolveDiveVerticalSpeed_상승은_수면에_가까워지면_스프링속도로_줄어든다()
        {
            const float floatY = -1.35f;
            float far = PlayerSwimRules.ResolveDiveVerticalSpeed(-4f, floatY, -10f, 2.5f, 6f, 6f);
            float near = PlayerSwimRules.ResolveDiveVerticalSpeed(floatY - 0.2f, floatY, -10f, 2.5f, 6f, 6f);
            Assert.That(far, Is.EqualTo(2.5f));
            Assert.That(near, Is.EqualTo(1.2f).Within(0.0001f));
        }

        [TestCase(0f, -0.6f, 0.8f, 30f, true)]    // 약 37도 아래를 보고 앞으로 헤엄침
        [TestCase(0f, -0.2f, 0.98f, 30f, false)]  // 거의 수평이면 수면 수영 유지
        [TestCase(0f, 0.5f, 0.87f, 30f, false)]   // 위를 보고 헤엄치면 잠수하지 않음
        [TestCase(0f, -0.3f, 0.1f, 30f, false)]   // 입력이 약하면 시작하지 않음
        public void IsDiveIntent_아래를_보고_충분히_헤엄칠때만_true(float x, float y, float z, float pitch, bool expected) =>
            Assert.That(PlayerSwimRules.IsDiveIntent(new Vector3(x, y, z), 0.5f, pitch), Is.EqualTo(expected));

        [TestCase(18f, 0f)]    // 기준 피치는 수평
        [TestCase(22f, 0f)]    // 여유 각도 안은 깊이를 유지
        [TestCase(40f, 24f)]   // (22-6)*1.5 → 아래로 하강
        [TestCase(-12f, -36f)] // (30-6)*1.5 → 위로 상승
        [TestCase(90f, 80f)]   // 상한
        public void ResolveSwimPitch_기준피치에서_벗어난만큼_증폭한다(float cameraPitch, float expected) =>
            Assert.That(PlayerSwimRules.ResolveSwimPitch(cameraPitch, 18f, 6f, 1.5f), Is.EqualTo(expected).Within(0.0001f));

        [TestCase(0f, -2f, 35f, true)]    // 수평 속도 없이 가라앉음
        [TestCase(3f, -2.5f, 35f, true)]  // 약 40도
        [TestCase(3f, -1f, 35f, false)]   // 완만한 하강은 엎드려 이동
        [TestCase(3f, 1f, 35f, false)]
        public void IsSteepDescent_하강각이_임계_이상일때만_true(float horizontal, float vertical, float angle, bool expected) =>
            Assert.That(PlayerSwimRules.IsSteepDescent(horizontal, vertical, angle), Is.EqualTo(expected));

        [TestCase(ActorStateId.Swim, false, false)]  // 수면 수영은 지면 판정이 없어도 회복
        [TestCase(ActorStateId.Airborne, false, true)]
        [TestCase(ActorStateId.Idle, true, false)]
        [TestCase(ActorStateId.Charge, true, true)]
        public void IsRecoveryBlockedState_수영중에는_회복을_막지_않는다(ActorStateId state, bool grounded, bool expected) =>
            Assert.That(UPlayGround.Components.PlayerStaminaRuntime.IsRecoveryBlockedState(state, grounded), Is.EqualTo(expected));

        [TestCase(ActorStateId.Idle, true)]
        [TestCase(ActorStateId.GroundMove, true)]
        [TestCase(ActorStateId.Airborne, true)]
        [TestCase(ActorStateId.Attack, false)]
        [TestCase(ActorStateId.Hit, false)]
        [TestCase(ActorStateId.Death, false)]
        [TestCase(ActorStateId.Dialogue, false)]
        public void CanEnterFrom_이동계_상태에서만_자동진입한다(ActorStateId from, bool expected) =>
            Assert.That(PlayerSwimRules.CanEnterFrom(from), Is.EqualTo(expected));

        [Test]
        public void 수영_태그는_레지스트리에_등록되어_있다()
        {
            Assert.That(MotionTags.SwimIdle.IsValid(), Is.True);
            Assert.That(MotionTags.SwimMove.IsValid(), Is.True);
            Assert.That(MotionTags.SwimFast.IsValid(), Is.True);
            Assert.That(MotionTags.SwimDiveMove.IsValid(), Is.True);
            Assert.That(MotionTags.SwimDiveDescend.IsValid(), Is.True);
            Assert.That(UPlayGround.Gameplay.Tag.GameplayTags.State_Swim.IsValid(), Is.True);
        }

        [TestCase(0.2f, 0.2f, true, 10f)]   // 삼각형 안
        [TestCase(2f, 2f, false, 0f)]       // 삼각형 밖
        public void TryGetSurfaceHeight_깊이와_무관하게_수면높이를_돌려준다(float x, float z, bool expected, float expectedY)
        {
            var root = new GameObject("Swim water test");
            root.SetActive(false);
            root.transform.position = new Vector3(0f, 10f, 0f);
            var mesh = new Mesh
            {
                vertices = new[] { Vector3.zero, Vector3.right, Vector3.forward },
                triangles = new[] { 0, 2, 1 }
            };
            var filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var water = root.AddComponent<FootstepWaterSurface>();
            water.surfaceMesh = filter;
            root.SetActive(true);
            // Edit Mode에서는 OnEnable/OnDisable이 자동 호출되지 않아 등록부 갱신을 직접 호출한다.
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(FootstepWaterSurface).GetMethod("OnEnable", flags).Invoke(water, null);
            try
            {
                // 발소리 판정(최대 0.55m)을 넘는 깊은 물에서도 높이를 얻어야 수영 진입이 가능하다.
                bool actual = FootstepWaterSurface.TryGetSurfaceHeight(new Vector3(x, 3f, z), out float surfaceY);
                Assert.That(actual, Is.EqualTo(expected));
                if (expected) Assert.That(surfaceY, Is.EqualTo(expectedY).Within(0.001f));
            }
            finally
            {
                typeof(FootstepWaterSurface).GetMethod("OnDisable", flags).Invoke(water, null);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
