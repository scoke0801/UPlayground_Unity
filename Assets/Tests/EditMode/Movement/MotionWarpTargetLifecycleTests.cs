using NUnit.Framework;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.MovementController;

namespace UPlayGround.Movement.Tests
{
    /// <summary>키별 타깃 등록과 전환이 현재 타깃의 생존 판정을 오염시키지 않는지 검사한다.</summary>
    public sealed class MotionWarpTargetLifecycleTests
    {
        private GameObject _owner;
        private WarpDamageTarget _primary;
        private WarpDamageTarget _secondary;
        private MotionWarpController _warp;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("워프 타깃 검사");
            _warp = _owner.AddComponent<MotionWarpController>();
            _primary = new GameObject("첫 번째 타깃").AddComponent<WarpDamageTarget>();
            _secondary = new GameObject("두 번째 타깃").AddComponent<WarpDamageTarget>();
            _primary.transform.position = Vector3.forward * 2f;
            _secondary.transform.position = Vector3.right * 2f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_primary.gameObject);
            Object.DestroyImmediate(_secondary.gameObject);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 비활성_키의_사망한_타깃을_등록해도_현재_조준은_유지한다(bool useTargetValue)
        {
            _warp.SetTarget(_primary.transform);
            _warp.BeginWarpWindow(RotationSettings());
            _secondary.Alive = false;

            if (useTargetValue)
                _warp.SetTarget("secondary", new MotionWarpTarget { anchor = _secondary.transform });
            else
                _warp.SetTarget("secondary", _secondary.transform);

            Assert.That(TryGetFacing(out var direction), Is.True);
            Assert.That(Vector3.Angle(direction, Vector3.forward), Is.LessThan(.01f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 키를_전환하면_새_타깃의_생존_상태를_사용한다(bool primaryAlive)
        {
            _primary.Alive = primaryAlive;
            _secondary.Alive = !primaryAlive;
            _warp.SetTarget("secondary", _secondary.transform);
            _warp.SetTarget(_primary.transform);
            _warp.BeginWarpWindow(RotationSettings(), "secondary");

            Assert.That(TryGetFacing(out _), Is.EqualTo(_secondary.Alive));
            _warp.BeginWarpWindow(RotationSettings());
            Assert.That(TryGetFacing(out _), Is.EqualTo(_primary.Alive));
        }

        private bool TryGetFacing(out Vector3 direction)
            => _warp.TryGetFacingDirection(Vector3.zero, true, .5f, 0f, 4f, 18f, out direction);

        private static MotionWarpWindowSettings RotationSettings()
        {
            var settings = MotionWarpWindowSettings.Default(.5f);
            settings.translationWeight = 0f;
            return settings;
        }

        private sealed class WarpDamageTarget : MonoBehaviour, IDamageable
        {
            public bool Alive = true;
            public CombatResult ReceiveHit(in HitRequest request) => default;
            public bool IsAlive() => Alive;
            public bool CanTakeDamage() => Alive;
            public Transform GetTransform() => transform;
            public void LockOn() { }
            public void UnLockOn() { }
            public float GetHealthPercent() => Alive ? 1f : 0f;
            public float GetCurrentHealth() => GetHealthPercent();
            public void ApplyHealingEffect(float healAmount) { }
        }
    }
}
