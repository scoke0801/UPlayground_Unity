#if UNITY_INCLUDE_TESTS
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.Editor.P09Builder.Tests
{
    /// <summary>원본 외형 보존, 잘못된 장비의 무변경 실패와 생성 경로 보호를 검사한다.</summary>
    public sealed class ExternalCharacterBuilderTests
    {
        private const string SourcePath = "Assets/ExternalAssets/Character/Honoka/Honoka.prefab";

        [Test]
        public void 슬롯_교체는_본과_자식_오브젝트를_끄지_않는다()
        {
            var root = new GameObject("Test");
            try
            {
                var a = new GameObject("A").AddComponent<MeshRenderer>();
                var b = new GameObject("B").AddComponent<MeshRenderer>();
                a.transform.SetParent(root.transform);
                b.transform.SetParent(a.transform);
                var visuals = root.AddComponent<CharacterEquipmentVisuals>();
                visuals.Configure(new[] { new CharacterEquipmentVisuals.Slot
                {
                    id = "coat", variants = new[]
                    {
                        new CharacterEquipmentVisuals.Variant { id = "a", renderers = new Renderer[] { a } },
                        new CharacterEquipmentVisuals.Variant { id = "b", renderers = new Renderer[] { b } }
                    }
                } });
                Assert.That(visuals.TryEquip("coat", "b"), Is.True);
                Assert.That(a.enabled, Is.False);
                Assert.That(b.enabled && b.gameObject.activeInHierarchy, Is.True);
                Assert.That(visuals.TryEquip("coat", "missing"), Is.False);
                Assert.That(b.enabled, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void 다른_슬롯이_같은_렌더러를_제어하면_부분_적용하지_않는다()
        {
            var root = new GameObject("Test");
            try
            {
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.enabled = false;
                var visuals = root.AddComponent<CharacterEquipmentVisuals>();
                var variant = new CharacterEquipmentVisuals.Variant { id = "on", renderers = new Renderer[] { renderer } };
                Assert.Throws<InvalidOperationException>(() => visuals.Configure(new[]
                {
                    new CharacterEquipmentVisuals.Slot { id = "a", variants = new[] { variant } },
                    new CharacterEquipmentVisuals.Slot { id = "b", variants = new[] { variant } }
                }));
                Assert.That(renderer.enabled, Is.False);
                Assert.That(visuals.Slots.Count, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void 외부_모델_조립은_얼굴_메시_재질_표정과_본을_보존한다()
        {
            var profile = CreateProfile();
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var source = profile.sourcePrefab.transform.Find("Body").GetComponent<SkinnedMeshRenderer>();
                var root = ExternalCharacterAssembler.CreateInstance(profile, scene);
                var body = root.transform.Find("Body").GetComponent<SkinnedMeshRenderer>();
                Assert.That(body.sharedMesh, Is.SameAs(source.sharedMesh));
                CollectionAssert.AreEqual(source.sharedMaterials, body.sharedMaterials);
                Assert.That(body.sharedMesh.blendShapeCount, Is.GreaterThan(0));
                for (int i = 0; i < body.sharedMesh.blendShapeCount; i++)
                    Assert.That(body.GetBlendShapeWeight(i), Is.EqualTo(source.GetBlendShapeWeight(i)));
                Assert.That(body.bones.All(b => b != null && b.IsChildOf(root.transform)), Is.True);
                var visuals = root.GetComponent<CharacterEquipmentVisuals>();
                Assert.That(visuals.TryEquip("coat", "off"), Is.True);
                Assert.That(root.transform.Find("Coat").GetComponent<Renderer>().enabled, Is.False);
                Assert.That(profile.sourcePrefab.transform.Find("Coat").GetComponent<Renderer>().enabled, Is.True);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 잘못된_후속_경로는_임시_인스턴스를_남기지_않는다()
        {
            var profile = CreateProfile();
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                profile.slots[0].variants[1].rendererPaths = new[] { "Missing/Coat" };
                Assert.Throws<InvalidOperationException>(() => ExternalCharacterAssembler.CreateInstance(profile, scene));
                Assert.That(scene.rootCount, Is.Zero);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 원본과_상위_경로에_저장할_수_없다()
        {
            Assert.Throws<InvalidOperationException>(() => ExternalCharacterAssembler.Build(null, SourcePath));
            Assert.Throws<InvalidOperationException>(() => ExternalCharacterAssembler.Build(null,
                ExternalCharacterAssembler.OutputFolder + "/../Overwrite.prefab"));
        }

        [Test]
        public void 잘못된_선택은_원본을_바꾸지_않고_거부한다()
        {
            var profile = CreateProfile();
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                Assert.Throws<InvalidOperationException>(() => ExternalCharacterAssembler.CreateInstance(profile, scene, new[] { 10 }));
                Assert.That(scene.rootCount, Is.Zero);
                Assert.That(profile.slots[0].defaultIndex, Is.Zero);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 장신구는_FBX_단위_보정_스케일을_보존한다()
        {
            var profile = AssetDatabase.LoadAssetAtPath<ExternalCharacterProfile>("Assets/10.Datas/Actor/ExternalModels/Honoka_Traveler.asset");
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = ExternalCharacterAssembler.CreateInstance(profile, scene);
                var pouch = root.GetComponentsInChildren<MeshRenderer>().Single(r => r.name.Contains("TravelerPouch"));
                Assert.That(pouch.bounds.size.x, Is.InRange(0.10f, 0.30f));
                Assert.That(pouch.bounds.size.y, Is.InRange(0.10f, 0.22f));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void 기존_출력은_GUID와_내용을_보존한다()
        {
            const string path = ExternalCharacterAssembler.OutputFolder + "/Honoka_Traveler_v2.prefab";
            string guid = AssetDatabase.AssetPathToGUID(path);
            string before = System.IO.File.ReadAllText(path);
            Assert.That(guid, Is.Not.Empty);
            Assert.Throws<InvalidOperationException>(() => ExternalCharacterAssembler.Build(null, path));
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
            Assert.That(System.IO.File.ReadAllText(path), Is.EqualTo(before));
        }

        private static ExternalCharacterProfile CreateProfile()
        {
            var profile = ScriptableObject.CreateInstance<ExternalCharacterProfile>();
            profile.sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            profile.slots = new[] { new ExternalCharacterProfile.EquipmentSlot
            {
                id = "coat", variants = new[]
                {
                    new ExternalCharacterProfile.EquipmentVariant { id = "on", rendererPaths = new[] { "Coat", "Coat/CoatBelt" } },
                    new ExternalCharacterProfile.EquipmentVariant { id = "off" }
                }
            } };
            return profile;
        }
    }
}
#endif
