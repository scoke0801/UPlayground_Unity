using System;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Content.Tests
{
    /// <summary>던전 지형 이식이 기존 구멍과 작업 범위 밖의 높이를 보존하는지 검사한다.</summary>
    public sealed class OpenWorldDungeonGroundTests
    {
        private Terrain _terrain;
        private TerrainData _data;
        private Type _authoring;
        private UnityEngine.SceneManagement.Scene _scene;

        [SetUp]
        public void SetUp()
        {
            _authoring = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UPlayGround.Editor.OpenWorldSliceAuthoring")).First(type => type != null);
            _scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            _data = new TerrainData { heightmapResolution = 129, size = new Vector3(128, 64, 128) };
            var heights = new float[129, 129];
            for (int z = 0; z < 129; z++)
            for (int x = 0; x < 129; x++) heights[z, x] = 4f / 64;
            _data.SetHeights(0, 0, heights);
            _terrain = Terrain.CreateTerrainGameObject(_data).GetComponent<Terrain>();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_terrain.gameObject, _scene);
            _terrain.transform.position = new Vector3(20000, 0, 20000);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_terrain.gameObject);
            UnityEngine.Object.DestroyImmediate(_data);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(_scene);
        }

        [Test]
        public void 입구_구멍과_지붕을_적용해도_기존_구멍과_외부_높이는_보존한다()
        {
            var holes = _data.GetHoles(0, 0, 128, 128);
            holes[2, 2] = false;
            _data.SetHoles(0, 0, holes);
            Apply("{\"dungeonGround\":[{\"id\":\"roof\",\"bounds\":{\"x\":20020,\"y\":20020,\"width\":20,\"height\":20},\"height\":12,\"transition\":8},{\"id\":\"entry\",\"bounds\":{\"x\":20024,\"y\":20020,\"width\":8,\"height\":8},\"isHole\":true}]}");
            Assert.That(_data.GetInterpolatedHeight(30f / 128, 30f / 128), Is.EqualTo(12).Within(.01));
            Assert.That(_data.GetInterpolatedHeight(5f / 128, 5f / 128), Is.EqualTo(4).Within(.01));
            Assert.That(_data.IsHole(28, 24), Is.True);
            Assert.That(_data.IsHole(2, 2), Is.True);
            Assert.That(_data.IsHole(35, 35), Is.False);
        }

        [Test]
        public void 원본_맵의_고저차를_복사하고_원본_에셋은_변경하지_않는다()
        {
            const string path = "Assets/ExternalAssets/Environment/LowPolyFantasyArena2/Terrain/LowPolyFantasyArena2.asset";
            var source = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            Assert.That(source, Is.Not.Null);
            float original = source.GetInterpolatedHeight(.5f, .5f);
            Apply("{\"dungeonGround\":[{\"id\":\"relief\",\"bounds\":{\"x\":20000,\"y\":20000,\"width\":128,\"height\":128},\"height\":3,\"sourceTerrain\":\"" + path + "\"}]}");
            Assert.That(_data.GetInterpolatedHeight(.5f, .5f), Is.EqualTo(original + 3).Within(.01));
            Assert.That(source.GetInterpolatedHeight(.5f, .5f), Is.EqualTo(original));
        }

        private void Apply(string json)
        {
            var recipeType = _authoring.GetNestedType("LandscapeRecipe", BindingFlags.NonPublic);
            var recipe = JsonUtility.FromJson(json, recipeType);
            _authoring.GetMethod("ApplyDungeonGround", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new[] { _terrain }, recipe });
        }

        [Test]
        public void 접근로의_양끝_높이가_연속_경사로_연결된다()
        {
            Apply("{\"dungeonGround\":[{\"id\":\"grade\",\"bounds\":{\"x\":20020,\"y\":20020,\"width\":40,\"height\":20},\"height\":5,\"endHeight\":15,\"hasSlope\":true,\"transition\":8}]}");
            Assert.That(_data.GetInterpolatedHeight(20f / 128, 30f / 128), Is.EqualTo(5).Within(.01));
            Assert.That(_data.GetInterpolatedHeight(40f / 128, 30f / 128), Is.EqualTo(10).Within(.01));
            Assert.That(_data.GetInterpolatedHeight(60f / 128, 30f / 128), Is.EqualTo(15).Within(.01));
        }
    }
}
