using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace UPlayGround.Content.Tests
{
    /// <summary>도로 저작 높이가 산체를 좁은 둑으로 변형하는 회귀를 차단한다.</summary>
    public sealed class OpenWorldRoadTerrainTests
    {
        [Test]
        public void 정지한_입자의_원점은_소품의_접지_범위에_들어가지_않는다()
        {
            var root = new GameObject("접지 검사");
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.transform.SetParent(root.transform);
            mesh.transform.position = new Vector3(600, 100, 500);
            var particles = new GameObject("정지한 입자");
            particles.transform.SetParent(root.transform);
            particles.AddComponent<ParticleSystem>();
            try
            {
                var authoring = Type.GetType("UPlayGround.Editor.OpenWorldSliceAuthoring, Assembly-CSharp-Editor", true);
                var bounds = (Bounds)authoring.GetMethod("GetPropMeshBounds", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { root });
                Assert.That(bounds.center, Is.EqualTo(mesh.transform.position));
                Assert.That(bounds.min.y, Is.EqualTo(99.5f).Within(0.001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void 산줄기의_최근접_선분이_바뀌어도_절개선이_생기지_않는다()
        {
            var authoring = Type.GetType("UPlayGround.Editor.OpenWorldSliceAuthoring, Assembly-CSharp-Editor", true);
            var formType = authoring.GetNestedType("Landform", BindingFlags.NonPublic);
            var form = JsonUtility.FromJson("{\"heightBlendDistance\":60,\"points\":[{\"x\":0,\"y\":0,\"z\":0},{\"x\":0,\"y\":10,\"z\":100},{\"x\":100,\"y\":30,\"z\":100}]}", formType);
            var method = authoring.GetMethod("GetLandformDistance", BindingFlags.NonPublic | BindingFlags.Static);
            object[] left = { new Vector3(9.99f, 0, 90), form, 0f };
            object[] right = { new Vector3(10.01f, 0, 90), form, 0f };
            method.Invoke(null, left); method.Invoke(null, right);
            Assert.That(Mathf.Abs((float)left[2] - (float)right[2]), Is.LessThan(0.02f));
        }

        [TestCase(true, 90, 10)]
        [TestCase(false, 90, 10.5f)]
        [TestCase(false, 0, 9.5f)]
        public void 도로는_지형을_따르거나_허용된_정지량만_적용한다(bool followTerrain, float target, float expected)
        {
            var authoring = Type.GetType("UPlayGround.Editor.OpenWorldSliceAuthoring, Assembly-CSharp-Editor", true);
            var recipeType = authoring.GetNestedType("CityRecipe", BindingFlags.NonPublic);
            string json = "{\"roadHalfWidth\":3,\"roadShoulder\":8,\"cities\":[],\"routes\":[{\"id\":\"test\",\"halfWidth\":3,\"followTerrain\":"
                + followTerrain.ToString().ToLowerInvariant() + ",\"points\":[{\"x\":16,\"y\":" + target
                + ",\"z\":4},{\"x\":16,\"y\":" + target
                + ",\"z\":28}]}],\"landscape\":{\"maximumRoadEarthwork\":0.5}}";
            var recipe = JsonUtility.FromJson(json, recipeType);
            var data = new TerrainData { heightmapResolution = 33, alphamapResolution = 32, size = new Vector3(32, 100, 32) };
            var heights = new float[33, 33];
            for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z, x] = 0.1f;
            data.SetHeights(0, 0, heights);
            var terrain = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>();
            try
            {
                authoring.GetMethod("StampRoads", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new[] { (object)terrain, recipe });
                Assert.That(data.GetHeight(16, 16), Is.EqualTo(expected).Within(0.01f));
                Assert.That(data.GetHeight(0, 16), Is.EqualTo(10).Within(0.01f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(terrain.gameObject);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }
    }
}
