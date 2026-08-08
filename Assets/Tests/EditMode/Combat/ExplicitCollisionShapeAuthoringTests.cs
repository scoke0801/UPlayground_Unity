using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data.Event;
using Motion = UPlayGround.Animation.Motion;

namespace UPlayGround.Combat.Tests
{
    /// <summary>
    /// 명시적 판정 형상(<see cref="CollisionSourceType.ExplicitShape"/>)이 저작 없이 켜지는 것을 막는다.
    ///
    /// 이 실패는 조용하다 — 예외도 경고도 없고, 그냥 액터 주변이 통째로 맞는다.
    /// <see cref="ExplicitCollisionShapeData"/>의 기본값이 radius 5 / box(10,3,10)이라
    /// 소스만 켜고 형상을 안 만지면 반경 5m 구체가 된다. 평타 하나에 이게 걸리면
    /// 그 모션을 공유하는 모든 무기의 Light/Entry/Swap 계열이 함께 광역이 된다.
    ///
    /// 실제로 Katana_Combo_Attack_1_1이 이 상태였고, 8개 무기의
    /// Light.00·Entry·EntryGroggy·EntryAirborne·SwapEvade·SwapSpecial이 전부 영향을 받았다.
    /// </summary>
    public sealed class ExplicitCollisionShapeAuthoringTests
    {
        /// <summary>기본값 그대로면 "저작하지 않았다"고 본다. ExplicitCollisionShapeData 참조.</summary>
        private const float DefaultRadius = 5f;
        private const float DefaultCapsuleHeight = 4f;

        private static IEnumerable<string> AllMotionSetPaths() =>
            AssetDatabase.FindAssets("t:MotionSetAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p);

        private static IEnumerable<MotionEventBase> AllEvents(MotionSetAsset asset)
        {
            MotionSet set = asset?.motionSet;
            if (set == null)
                yield break;

            foreach (MotionEventBase e in set.globalEvents ?? Enumerable.Empty<MotionEventBase>())
                yield return e;
            foreach (Motion motion in set.motions ?? Enumerable.Empty<Motion>())
                foreach (MotionEventBase e in motion?.events ?? Enumerable.Empty<MotionEventBase>())
                    yield return e;
            foreach (MotionLayer layer in set.layers ?? Enumerable.Empty<MotionLayer>())
                foreach (Motion motion in layer?.motions ?? Enumerable.Empty<Motion>())
                    foreach (MotionEventBase e in motion?.events ?? Enumerable.Empty<MotionEventBase>())
                        yield return e;
        }

        [Test]
        public void 명시적_판정_형상은_기본값인_채로_켜져_있지_않다()
        {
            var failures = new List<string>();

            foreach (string path in AllMotionSetPaths())
            {
                var asset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
                if (asset == null)
                    continue;

                foreach (BeginCollisionEvent e in AllEvents(asset).OfType<BeginCollisionEvent>())
                {
                    if (e.collisionSource != CollisionSourceType.ExplicitShape)
                        continue;

                    ExplicitCollisionShapeData shape = e.explicitShape;
                    if (shape == null)
                    {
                        failures.Add($"{path}: ExplicitShape인데 형상이 null이다");
                        continue;
                    }

                    bool untouched = shape.shapeType switch
                    {
                        CollisionShapeType.Sphere => Mathf.Approximately(shape.radius, DefaultRadius),
                        CollisionShapeType.Capsule =>
                            Mathf.Approximately(shape.radius, DefaultRadius)
                            && Mathf.Approximately(shape.capsuleHeight, DefaultCapsuleHeight),
                        CollisionShapeType.Box => shape.boxSize == new Vector3(10f, 3f, 10f),
                        _ => false,
                    };

                    if (untouched)
                    {
                        failures.Add(
                            $"{path}: {shape.shapeType} 형상이 기본값 그대로다 "
                            + $"(radius={shape.radius}, box={shape.boxSize}, capsule={shape.capsuleHeight}). "
                            + "저작하지 않았다면 collisionSource를 AttachedHitboxGroup으로 되돌려라.");
                    }
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }
    }
}
