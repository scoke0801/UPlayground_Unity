using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Gameplay.World.LivingWorld;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.EditorTools.Tests
{
    /// <summary>전수 연결의 LOD 단위와 실패 시 사용자 배치 보존을 검사한다.</summary>
    public sealed class VegetationReactionAuthoringTests
    {
        private Scene _scene;
        private VegetationReactionCatalogSO _catalog;
        private ReactiveEnvironmentDefinitionSO _profile;
        private Material _material;
        private Mesh _mesh;
        private string _meshPath;
        private string _prefabPath;
        private string _parentMaterialPath, _variantMaterialPath;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
            _catalog = ScriptableObject.CreateInstance<VegetationReactionCatalogSO>();
            _profile = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ReactiveEnvironmentDefinitionSO>("Assets/10.Datas/Environment/Profiles/ArenaTree.asset"));
            _catalog.protectedVegetation = _profile;
            _material = new Material(Shader.Find("UPlayGround/Environment/Reactive Toon"));
            _meshPath = "Assets/VegetationReactionTest_" + Guid.NewGuid().ToString("N") + ".asset";
            _prefabPath = _meshPath + ".prefab";
            _parentMaterialPath = _meshPath + ".parent.mat";
            _variantMaterialPath = _meshPath + ".variant.mat";
            _mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up }, triangles = new[] { 0, 1, 2 } };
            _mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(_mesh, _meshPath);
            _catalog.sources.Add(new VegetationReactionCatalogSO.Source { meshGuid = AssetDatabase.AssetPathToGUID(_meshPath), definition = _profile });
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.ClosePreviewScene(_scene);
            if (AssetDatabase.LoadMainAssetAtPath(_prefabPath) != null) AssetDatabase.DeleteAsset(_prefabPath);
            if (AssetDatabase.LoadMainAssetAtPath(_variantMaterialPath) != null) AssetDatabase.DeleteAsset(_variantMaterialPath);
            if (AssetDatabase.LoadMainAssetAtPath(_parentMaterialPath) != null) AssetDatabase.DeleteAsset(_parentMaterialPath);
            AssetDatabase.DeleteAsset(_meshPath);
            UnityEngine.Object.DestroyImmediate(_catalog);
            UnityEngine.Object.DestroyImmediate(_profile);
            UnityEngine.Object.DestroyImmediate(_material);
        }

        [Test]
        public void BatchedNewIdsKeepExistingIdentityAndMatchIndividualOwnerKeys()
        {
            var existing = NewRoot("기존 ID").AddComponent<SceneEntityId>();
            string original = existing.Guid;
            SceneEntityId first = null, second = null;
            SceneEntityId.EditorInitializeNewIds(() =>
            {
                first = NewRoot("새 ID 1").AddComponent<SceneEntityId>();
                second = NewRoot("새 ID 2").AddComponent<SceneEntityId>();
            });
            Assert.That(existing.Guid, Is.EqualTo(original));
            Assert.That(first.HasGuid && second.HasGuid, Is.True);
            Assert.That(first.Guid, Is.Not.EqualTo(second.Guid));
            foreach (var identity in new[] { first, second })
            {
                string guid = identity.Guid;
                var data = new SerializedObject(identity);
                Assert.That(data.FindProperty("_ownerKey").stringValue,
                    Is.EqualTo(GlobalObjectId.GetGlobalObjectIdSlow(identity).ToString()));
                typeof(SceneEntityId).GetMethod("OnValidate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(identity, null);
                Assert.That(identity.Guid, Is.EqualTo(guid));
            }
        }

        [Test]
        public void FailedBatchDoesNotLeaveIdentityValidationDeferred()
        {
            SceneEntityId unfinished = null;
            Assert.Throws<InvalidOperationException>(() => SceneEntityId.EditorInitializeNewIds(() =>
            {
                unfinished = NewRoot("중단 직전 새 ID").AddComponent<SceneEntityId>();
                throw new InvalidOperationException("중단");
            }));
            Assert.That(new SerializedObject(unfinished).FindProperty("_ownerKey").stringValue,
                Is.EqualTo(GlobalObjectId.GetGlobalObjectIdSlow(unfinished).ToString()));
            var identity = NewRoot("다음 일반 저작").AddComponent<SceneEntityId>();
            Assert.That(identity.HasGuid, Is.True);
            Assert.That(new SerializedObject(identity).FindProperty("_ownerKey").stringValue,
                Is.EqualTo(GlobalObjectId.GetGlobalObjectIdSlow(identity).ToString()));
        }

        [Test]
        public void CatalogGroupsAllLodsWithoutUsingObjectNames()
        {
            var root = NewRoot("분류와 무관한 이름");
            root.AddComponent<LODGroup>();
            AddRenderer(root.transform, "01"); AddRenderer(root.transform, "02");
            var plants = VegetationReactionAuthoring.Collect(_scene, _catalog);
            Assert.That(plants.Count, Is.EqualTo(1));
            Assert.That(plants[0].root, Is.SameAs(root));
            Assert.That(plants[0].surfaces.Count, Is.EqualTo(2));
        }

        [Test]
        public void SiblingLodGroupsInOnePrefabShareOneReactionRoot()
        {
            var root = NewRoot("한 식물");
            AddRenderer(root.transform, "잎").gameObject.AddComponent<LODGroup>();
            AddRenderer(root.transform, "꽃").gameObject.AddComponent<LODGroup>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, _prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _scene);
            var plants = VegetationReactionAuthoring.Collect(_scene, _catalog);
            Assert.That(plants.Count, Is.EqualTo(1));
            Assert.That(plants[0].root, Is.SameAs(instance));
            Assert.That(plants[0].surfaces.Count, Is.EqualTo(2));
        }

        [Test]
        public void PhysicalColliderAndTriggerShareTheSameReactionTarget()
        {
            var root = NewRoot("원형 충돌 보존");
            var renderer = AddRenderer(root.transform, "형상");
            var physical = root.AddComponent<BoxCollider>();
            var weather = NewRoot("날씨").AddComponent<EnvironmentController>();
            var plants = new List<VegetationReactionAuthoring.Plant>
            {
                new() { root = root, definition = _profile, surfaces = new List<Renderer> { renderer } }
            };
            VegetationReactionAuthoring.ApplyPlants(plants, _catalog, weather);
            var reaction = root.GetComponent<ReactiveEnvironmentObject>();
            Assert.That(reaction.CanReceiveCollider(physical), Is.True);
            Assert.That(physical.isTrigger, Is.False);
            var trigger = root.transform.Find("EnvironmentReactionHit").GetComponent<Collider>();
            Assert.That(trigger.isTrigger, Is.True);
            Assert.That(((BoxCollider)trigger).size.z, Is.GreaterThan(0), "평면 메시 입력 범위의 깊이");
            Assert.That(reaction.CanReceiveCollider(trigger), Is.True);
        }

        [Test]
        public void MaterialVariantCopyKeepsEffectivePropertiesWithoutKeepingShaderParent()
        {
            var parent = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            parent.SetTexture("_BaseMap", Texture2D.whiteTexture);
            parent.SetColor("_BaseColor", Color.red);
            AssetDatabase.CreateAsset(parent, _parentMaterialPath);
            var variant = new Material(parent.shader) { parent = parent };
            variant.SetColor("_BaseColor", Color.green);
            AssetDatabase.CreateAsset(variant, _variantMaterialPath);
            var copy = VegetationReactionCatalogBuilder.CreateMaterialCopy(variant, _material.shader);
            try
            {
                Assert.That(copy.isVariant, Is.False);
                Assert.That(copy.shader, Is.SameAs(_material.shader));
                Assert.That(copy.GetColor("_BaseColor"), Is.EqualTo(variant.GetColor("_BaseColor")));
                Assert.That(copy.GetTexture("_BaseMap"), Is.SameAs(variant.GetTexture("_BaseMap")));
                Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(variant.parent)),
                    Is.EqualTo(AssetDatabase.AssetPathToGUID(_parentMaterialPath)));
                Assert.That(parent.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
            }
            finally { UnityEngine.Object.DestroyImmediate(copy); }
        }

        [Test]
        public void FailureAfterFirstPlantRollsBackWholeBatch()
        {
            var first = NewRoot("처음"); var renderer = AddRenderer(first.transform, "형상");
            var identity = first.AddComponent<SceneEntityId>(); identity.EditorSetGuid("existing-guid");
            var second = NewRoot("실패"); var secondRenderer = AddRenderer(second.transform, "형상");
            var weather = NewRoot("날씨").AddComponent<EnvironmentController>();
            var plants = new List<VegetationReactionAuthoring.Plant>
            {
                new() { root = first, definition = _profile, surfaces = new List<Renderer> { renderer } },
                new() { root = second, definition = null, surfaces = new List<Renderer> { secondRenderer } }
            };
            Assert.Throws<ArgumentException>(() => VegetationReactionAuthoring.ApplyPlants(plants, _catalog, weather));
            Assert.That(first.GetComponent<ReactiveEnvironmentObject>(), Is.Null);
            Assert.That(first.transform.Find("ReactionSamples"), Is.Null);
            Assert.That(first.transform.Find("EnvironmentReactionHit"), Is.Null);
            Assert.That(identity.Guid, Is.EqualTo("existing-guid"));
            Assert.That(renderer.sharedMaterial, Is.SameAs(_material));
        }

        private GameObject NewRoot(string name)
        {
            var root = new GameObject(name); SceneManager.MoveGameObjectToScene(root, _scene); return root;
        }

        private MeshRenderer AddRenderer(Transform parent, string name)
        {
            var child = NewRoot(name); child.transform.SetParent(parent, false);
            child.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var renderer = child.AddComponent<MeshRenderer>(); renderer.sharedMaterial = _material; return renderer;
        }
    }
}
