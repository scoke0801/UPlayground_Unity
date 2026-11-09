using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.World;

namespace UPlayGround.Content.Tests
{
    /// <summary>수영 수면과 광학 영역의 경계·비활성화·연속 전환을 검사한다.</summary>
    public sealed class UnderwaterEnvironmentTests
    {
        [TestCase(-1, 0)]
        [TestCase(0, 0)]
        [TestCase(0.09f, 0.5f)]
        [TestCase(0.18f, 1)]
        [TestCase(10, 1)]
        public void 수면_진입_가중치는_연속이고_범위가_제한된다(float depth, float expected)
        {
            var profile = ScriptableObject.CreateInstance<UnderwaterProfileSO>();
            try { Assert.That(profile.EvaluateImmersion(depth), Is.EqualTo(expected).Within(0.0001)); }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 실제_삼각형_외부와_수면_위는_수중으로_판정하지_않는다()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var go = new GameObject("UnderwaterRegionTest");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            var mesh = new Mesh
            {
                vertices = new[] { Vector3.zero, new Vector3(10, 0, 0), new Vector3(0, 0, 10) },
                triangles = new[] { 0, 2, 1 }
            };
            var profile = ScriptableObject.CreateInstance<UnderwaterProfileSO>();
            try
            {
                go.transform.SetPositionAndRotation(new Vector3(10000, 8, 10000), Quaternion.Euler(0, 30, 0));
                go.transform.localScale = Vector3.one * 2;
                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                Type surfaceType = Type.GetType("UPlayGround.Components.FootstepWaterSurface, UPlayGround.Actor", true);
                var surface = go.AddComponent(surfaceType);
                surfaceType.GetField("surfaceMesh").SetValue(surface, filter);
                surfaceType.GetMethod("RefreshMesh").Invoke(surface, null);
                Type regionType = Type.GetType("UPlayGround.Gameplay.World.UnderwaterRegion, Assembly-CSharp", true);
                var region = (Behaviour)go.AddComponent(regionType);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                regionType.GetField("_surface", flags).SetValue(region, surface);
                regionType.GetField("_profile", flags).SetValue(region, profile);
                MethodInfo resolve = regionType.GetMethod("TryResolve");
                Func<Vector3, bool> sample = local =>
                {
                    object[] args = { go.transform.TransformPoint(local), null, 0f };
                    bool result = (bool)resolve.Invoke(null, args);
                    if (result) Assert.That((float)args[2], Is.EqualTo(8).Within(0.001));
                    return result;
                };
                Assert.IsTrue(sample(new Vector3(2, -1, 2)));
                Assert.IsFalse(sample(new Vector3(8, -1, 8)), "AABB 내부라도 수면 삼각형 밖은 육지다.");
                Assert.IsFalse(sample(new Vector3(2, 1, 2)));
                region.enabled = false;
                Assert.IsFalse(sample(new Vector3(2, -1, 2)));
                region.enabled = true;
                Assert.IsTrue(sample(new Vector3(2, -1, 2)));
                ((Behaviour)surface).enabled = false;
                Assert.IsFalse(sample(new Vector3(2, -1, 2)));
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
