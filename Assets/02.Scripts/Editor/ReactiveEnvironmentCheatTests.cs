using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Manager.World;

namespace UPlayGround.EditorTools.Tests
{
    /// <summary>치트 복제의 지역·ID 격리와 잘못된 입력 거부를 검사한다.</summary>
    public sealed class ReactiveEnvironmentCheatTests
    {
        [Test]
        public void SpawnPreservesPrefabAndAssignsIndependentIdsInOriginScene()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            RegionOwnership.Register(scene, "environment-cheat-test");
            var origin = new GameObject("Origin");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(origin, scene);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/Environment/ReactiveEnvironmentCrateSample.prefab");

            try
            {
                Assert.That(prefab, Is.Not.Null);
                string originalId = prefab.GetComponent<SceneEntityId>().Guid;
                var first = ReactiveEnvironmentCheats.Spawn(prefab, origin.transform, Vector3.right, Quaternion.identity, out string error);
                Assert.That(error, Is.Null);
                var second = ReactiveEnvironmentCheats.Spawn(prefab, origin.transform, Vector3.left, Quaternion.identity, out error);
                Assert.That(error, Is.Null);
                Assert.That(first.gameObject.scene, Is.EqualTo(scene));
                Assert.That(second.gameObject.scene, Is.EqualTo(scene));
                Assert.That(first.GetComponent<SceneEntityId>().Guid, Is.Not.Empty);
                Assert.That(second.GetComponent<SceneEntityId>().Guid, Is.Not.EqualTo(first.GetComponent<SceneEntityId>().Guid));
                Assert.That(prefab.GetComponent<SceneEntityId>().Guid, Is.EqualTo(originalId));
                Assert.That(first.transform.position, Is.EqualTo(Vector3.right));
                Assert.That(scene.GetRootGameObjects().Length, Is.EqualTo(3), "임시 부모나 다른 씬에 복제본을 남기지 않는다");
            }
            finally
            {
                RegionOwnership.Unregister(scene);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void SpawnRejectsNonPrefabAndMissingRegion()
        {
            var source = new GameObject("NotAPrefab");
            try
            {
                Assert.That(ReactiveEnvironmentCheats.Spawn(source, source.transform, Vector3.zero, Quaternion.identity, out string error), Is.Null);
                Assert.That(error, Is.Not.Empty);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/03.Prefabs/Environment/ReactiveEnvironmentCrateSample.prefab");
                Assert.That(ReactiveEnvironmentCheats.Spawn(prefab, null, Vector3.zero, Quaternion.identity, out error), Is.Null);
                Assert.That(error, Is.Not.Empty);
            }
            finally { Object.DestroyImmediate(source); }
        }
    }
}
