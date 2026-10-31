using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPlayGround.Content.Tests
{
    /// <summary>지형 재조형 뒤 저장 위치의 접지와 구조물 위 위치 보존을 검사한다.</summary>
    public sealed class OpenWorldSurfaceRecoveryTests
    {
        private GameObject _terrainObject;
        private TerrainData _data;
        private Component _world;
        private MethodInfo _resolve;

        [SetUp]
        public void SetUp()
        {
            _data = new TerrainData { heightmapResolution = 33, size = new Vector3(100, 20, 100) };
            var heights = new float[33, 33];
            for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z, x] = 0.5f;
            _data.SetHeights(0, 0, heights);
            var terrain = Terrain.CreateTerrainGameObject(_data).GetComponent<Terrain>();
            _terrainObject = terrain.gameObject;
            terrain.transform.position = new Vector3(20000, 0, 20000);
            var root = new GameObject("저장 접지 검사");
            var type = Type.GetType("UPlayGround.Manager.World.ContinuousWorldController, Assembly-CSharp", true);
            _world = root.AddComponent(type);
            type.GetField("SurfaceRecoveryBounds").SetValue(_world, new Rect(20000, 20000, 100, 100));
            type.GetField("SurfaceRecoveryTerrains").SetValue(_world, new[] { terrain });
            _resolve = type.GetMethod("TryResolveSurfacePosition");
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null) UnityEngine.Object.DestroyImmediate(_world.gameObject);
            if (_terrainObject != null) UnityEngine.Object.DestroyImmediate(_terrainObject);
            UnityEngine.Object.DestroyImmediate(_data);
        }

        [TestCase(30)]
        [TestCase(4)]
        public void 이전_지형의_공중과_지중_위치를_복구한다(float height)
        {
            object[] args = { new Vector3(20050, height, 20050), Vector3.zero };
            Assert.That((bool)_resolve.Invoke(_world, args), Is.True);
            Assert.That(((Vector3)args[1]).y, Is.EqualTo(10.05f).Within(0.02f));
            Assert.That(((Vector3)args[1]).x, Is.EqualTo(20050));
        }

        [Test]
        public void 변경_구역_밖의_저장_위치는_유지한다()
        {
            object[] args = { new Vector3(19900, 30, 20050), Vector3.zero };
            Assert.That((bool)_resolve.Invoke(_world, args), Is.False);
            Assert.That((Vector3)args[1], Is.EqualTo(args[0]));
        }

        [Test]
        public void 유효한_교량_위_저장_위치는_유지한다()
        {
            var bridge = new GameObject("교량 충돌면");
            bridge.transform.SetParent(_world.transform);
            bridge.transform.position = new Vector3(20050, 15, 20050);
            bridge.AddComponent<BoxCollider>().size = new Vector3(8, 1, 8);
            Physics.SyncTransforms();
            object[] args = { new Vector3(20050, 15.55f, 20050), Vector3.zero };
            Assert.That((bool)_resolve.Invoke(_world, args), Is.False);
        }

        [TestCase(6.5f)]
        [TestCase(9f)]
        public void 지하층의_공중_저장은_덮개_지형이_아닌_내부_바닥으로_복구한다(float savedHeight)
        {
            var floor = new GameObject("지하 바닥");
            floor.transform.SetParent(_world.transform);
            floor.transform.position = new Vector3(20050, 4, 20050);
            floor.AddComponent<BoxCollider>().size = new Vector3(8, 1, 8);
            Physics.SyncTransforms();
            object[] args = { new Vector3(20050, savedHeight, 20050), Vector3.zero };
            Assert.That((bool)_resolve.Invoke(_world, args), Is.True);
            Assert.That(((Vector3)args[1]).y, Is.EqualTo(4.55f).Within(0.02f));
        }
    }
}
