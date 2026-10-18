using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Party;
using Motion = UPlayGround.Animation.Motion;

namespace UPlayGround.Combat.Tests
{
    /// <summary>조준 범위·타격 블렌드·실제 모델 연결의 회귀를 검사한다.</summary>
    public sealed class WeaponAimTests
    {
        [Test]
        public void Correction_RespectsAngleBudgetAndWeight()
        {
            Quaternion correction = WeaponAimMath.GetCorrection(Vector3.forward, Vector3.right, 30f, 0.5f);
            Assert.That(Quaternion.Angle(Quaternion.identity, correction), Is.EqualTo(15f).Within(0.01f));
            Assert.That(Vector3.Angle(correction * Vector3.forward, Vector3.right), Is.EqualTo(75f).Within(0.01f));
        }

        [Test]
        public void Correction_AlignsReachableAxisWithoutChangingRoll()
        {
            Vector3 target = Quaternion.AngleAxis(20f, Vector3.up) * Vector3.forward;
            Quaternion correction = WeaponAimMath.GetCorrection(Vector3.forward, target, 30f, 1f);
            Assert.That(Vector3.Angle(correction * Vector3.forward, target), Is.LessThan(0.01f));
            Assert.That(Vector3.Angle(correction * Vector3.up, Vector3.up), Is.LessThan(0.01f));
        }

        [Test]
        public void DegenerateAndRearTargets_DoNotTwistTheArm()
        {
            Assert.That(WeaponAimMath.GetCorrection(Vector3.up, Vector3.zero, 30f, 1f), Is.EqualTo(Quaternion.identity));
            Assert.That(WeaponAimMath.EvaluateAngleWeight(Vector3.forward, Vector3.back, 35f, 75f), Is.Zero);
            Assert.That(WeaponAimMath.EvaluateAngleWeight(Vector3.zero, Vector3.forward, 35f, 75f), Is.Zero);
            Vector3 edge = Quaternion.AngleAxis(55f, Vector3.up) * Vector3.forward;
            Assert.That(WeaponAimMath.EvaluateAngleWeight(Vector3.forward, edge, 35f, 75f), Is.EqualTo(0.5f).Within(0.001f));
        }

        [TestCase(0.7f, 0f)]
        [TestCase(0.9f, 0.5f)]
        [TestCase(1f, 1f)]
        [TestCase(1.1f, 1f)]
        [TestCase(1.3f, 0.5f)]
        [TestCase(1.5f, 0f)]
        public void Window_BlendsAroundTheAuthoredHit(float time, float expected)
        {
            Assert.That(WeaponAimMath.EvaluateWindow(time, 1f, 1.2f, 0.2f, 0.2f), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void ZeroDurationWindow_IsFinite()
        {
            Assert.That(WeaponAimMath.EvaluateWindow(1f, 1f, 1f, 0f, 0f), Is.EqualTo(1f));
            Assert.That(WeaponAimMath.EvaluateWindow(1.01f, 1f, 1f, 0f, 0f), Is.Zero);
        }

        [Test]
        public void Timeline_HonorsOptOutAndExplicitShapes()
        {
            var set = new MotionSet();
            var hit = new BeginCollisionEvent { startTime = 1f, endTime = 1.2f };
            set.globalEvents.Add(hit);
            Assert.That(WeaponAimMath.EvaluateMotionWeight(set, 1.1f), Is.EqualTo(1f));
            hit.suppressWeaponAim = true;
            Assert.That(WeaponAimMath.EvaluateMotionWeight(set, 1.1f), Is.Zero);
            hit.suppressWeaponAim = false;
            hit.collisionSource = CollisionSourceType.ExplicitShape;
            Assert.That(WeaponAimMath.EvaluateMotionWeight(set, 1.1f), Is.Zero);
        }

        [Test]
        public void Timeline_UsesLocalEventOffsetAfterPreviousClip()
        {
            var clip = new AnimationClip();
            try
            {
                clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
                var set = new MotionSet();
                set.motions.Add(new Motion { id = "first", motionClip = clip });
                var second = new Motion { id = "second", motionClip = clip };
                second.events.Add(new BeginCollisionEvent { startTime = 0.25f, endTime = 0.4f });
                set.motions.Add(second);
                Assert.That(WeaponAimMath.EvaluateMotionWeight(set, 0.3f), Is.Zero);
                Assert.That(WeaponAimMath.EvaluateMotionWeight(set, 1.3f), Is.EqualTo(1f));
            }
            finally { UnityEngine.Object.DestroyImmediate(clip); }
        }

        [Test]
        public void PivotCorrection_AccountsForMovingWeaponOrigin()
        {
            Vector3 pivot = new Vector3(0f, 1.5f, 0f);
            Vector3 origin = pivot + new Vector3(0.6f, -0.2f, 0.4f);
            Vector3 target = new Vector3(0.9f, 2f, 3f);
            Quaternion correction = WeaponAimMath.GetPivotCorrection(pivot, origin, Vector3.forward, target, 180f, 1f);
            Vector3 correctedOrigin = pivot + correction * (origin - pivot);
            Assert.That(Vector3.Angle(correction * Vector3.forward, target - correctedOrigin), Is.LessThan(0.02f));
            Assert.That(WeaponAimMath.GetPivotCorrection(pivot, origin, Vector3.forward, pivot, 180f, 1f),
                Is.EqualTo(Quaternion.identity));
        }

        [Test]
        public void Projectile_ReleasesAfterShotInsteadOfEventEnd()
        {
            var set = new MotionSet();
            var shot = new SpawnProjectileEvent { startTime = 1f, endTime = 5f, weaponAimBlendOut = 0.1f };
            set.globalEvents.Add(shot);
            Assert.That(WeaponAimMath.EvaluateMotionWeight(set, 1f), Is.EqualTo(1f));
            Assert.That(WeaponAimMath.EvaluateMotionWeight(set, 1.2f), Is.Zero);
            shot.projectTargetToGround = true;
            Assert.That(WeaponAimMath.EvaluateMotionWeight(set, 1f), Is.Zero);
        }

        [TestCase("H09", WeaponType.Staff)]
        [TestCase("Reine", WeaponType.Spear)]
        public void Model_HasMatchingAimConstraintAndCombatData(string modelName, WeaponType weaponType)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/03.Prefabs/Actor/Player/Models/PlayerModel_{modelName}.prefab");
            Assert.That(prefab, Is.Not.Null);
            var animator = prefab.GetComponentInChildren<PlayerActorAnimator>(true);
            Assert.That(animator, Is.Not.Null);
            var serialized = new SerializedObject(animator);
            var aim = serialized.FindProperty("_weaponAim");
            Assert.That(aim.FindPropertyRelative("enabled").boolValue, Is.True);
            Assert.That(aim.FindPropertyRelative("weaponType").intValue, Is.EqualTo((int)weaponType));
            Assert.That(aim.FindPropertyRelative("weaponConstraint").objectReferenceValue, Is.Not.Null);
            Assert.That(aim.FindPropertyRelative("localAimAxis").vector3Value.sqrMagnitude, Is.GreaterThan(0.99f));
            var definition = AssetDatabase.LoadAssetAtPath<PlayerCharacterDefinitionSO>(
                $"Assets/10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_{modelName}.asset");
            Assert.That(definition.defaultWeaponType, Is.EqualTo(weaponType));
            Assert.That(definition.abilitySet, Is.Not.Null);
        }
    }
}
