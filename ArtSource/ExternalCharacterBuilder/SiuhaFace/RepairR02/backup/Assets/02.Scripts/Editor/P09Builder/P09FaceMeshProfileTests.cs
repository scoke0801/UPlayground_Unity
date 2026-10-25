#if UNITY_INCLUDE_TESTS
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

namespace UPlayGround.Editor.P09Builder.Tests
{
    /// <summary>얼굴 교체와 P09 장비 선택의 독립성 및 실패 시 원본 보존을 검사한다.</summary>
    public sealed class P09FaceMeshProfileTests
    {
        private const string PresetPath = "Assets/10.Datas/Actor/ExternalModels/Daon_P09_BuildPreset.asset";

        [Test]
        public void 헤어와_의상_교체에도_새_얼굴과_P09_표정_계약을_유지한다()
        {
            var saved = AssetDatabase.LoadAssetAtPath<BuildPreset>(PresetPath);
            var config = JsonUtility.FromJson<CharacterBuildConfig>(JsonUtility.ToJson(saved.config));
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PathConfig.BasePrefabFemaleNoPhysics));
            try
            {
                var catalog = new P09AssetCatalog();
                catalog.Refresh();
                foreach (int index in new[] { 1, 3, 11 })
                {
                    config.HairStyleSo = AssetDatabase.LoadAssetAtPath<ScriptableObject>(PathConfig.CatalogRoot + $"/HairStyle/HairStyleEditPartData_{index:00}.asset");
                    AppearanceApplier.Apply(root, config, catalog);
                    var faces = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => r.name.StartsWith("Female_Face_")).ToArray();
                    Assert.That(faces.Length, Is.EqualTo(3));
                    foreach (var face in faces)
                    {
                        Assert.That(face.sharedMesh, Is.SameAs(config.FaceMeshProfile.mesh));
                        Assert.That(face.sharedMesh.GetBlendShapeIndex("Eye_Blink"), Is.EqualTo(0));
                        Assert.That(face.sharedMesh.GetBlendShapeIndex("Lip_aa"), Is.GreaterThanOrEqualTo(0));
                    }
                    Assert.That(root.GetComponentsInChildren<SkinnedMeshRenderer>().Count(r => r.name.StartsWith("Hair_")), Is.EqualTo(1));
                }
                config.ArmorSelections.Set(BuilderArmorSlot.Head, null);
                config.ArmorSelections.Set(BuilderArmorSlot.Chest, catalog.Chests[1]);
                AppearanceApplier.Apply(root, config, catalog);
                Assert.That(root.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name.StartsWith("Female_Face_")).sharedMesh,
                    Is.SameAs(config.FaceMeshProfile.mesh));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void 뒤쪽_대상이_잘못되어도_앞쪽_얼굴을_변경하지_않는다()
        {
            var saved = AssetDatabase.LoadAssetAtPath<BuildPreset>(PresetPath);
            var profile = UnityEngine.Object.Instantiate(saved.config.FaceMeshProfile);
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PathConfig.BasePrefabFemaleNoPhysics));
            try
            {
                var first = root.transform.Find(profile.rendererPaths[0]).GetComponent<SkinnedMeshRenderer>();
                var before = first.sharedMesh;
                profile.rendererPaths = new[] { profile.rendererPaths[0], "Missing/Face" };
                Assert.Throws<InvalidOperationException>(() => profile.Apply(root));
                Assert.That(first.sharedMesh, Is.SameAs(before));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 헤어_부착은_반복_적용해도_누적되지_않고_머리_좌표를_따른다()
        {
            var saved = AssetDatabase.LoadAssetAtPath<BuildPreset>(PresetPath);
            var profile = UnityEngine.Object.Instantiate(saved.config.FaceMeshProfile);
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PathConfig.BasePrefabFemaleNoPhysics));
            try
            {
                var constraint = root.GetComponentsInChildren<ParentConstraint>(true)
                    .First(c => c.name == "Constraint_Head");
                var offset = new Vector3(-0.02f, 0.01f, 0f);
                profile.hairAttachments = new[] { new P09FaceMeshProfileSO.HairAttachment
                {
                    constraintPath = AnimationUtility.CalculateTransformPath(constraint.transform, root.transform),
                    positionOffset = offset,
                    localScale = new Vector3(0.88f, 1f, 1f)
                }};
                var hair = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .First(r => r.bones.Contains(constraint.transform));
                var originalMesh = hair.sharedMesh;
                var source = constraint.GetSource(0).sourceTransform;
                source.localRotation *= Quaternion.Euler(0, 35, -20);
                profile.Apply(root);
                profile.Apply(root);
                Assert.That(constraint.GetTranslationOffset(0), Is.EqualTo(offset));
                Assert.That(Vector3.Distance(constraint.transform.position, source.TransformPoint(offset)), Is.LessThan(0.00001f));
                Assert.That(constraint.locked, Is.True);
                Assert.That(constraint.transform.localScale, Is.EqualTo(new Vector3(0.88f, 1f, 1f)));
                Assert.That(hair.sharedMesh, Is.SameAs(originalMesh));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 배율이_없는_기존_헤어_설정은_원래_크기로_읽는다()
        {
            var attachment = JsonUtility.FromJson<P09FaceMeshProfileSO.HairAttachment>("{\"constraintPath\":\"Hair\"}");
            Assert.That(attachment.localScale, Is.EqualTo(Vector3.one));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void 유효하지_않은_헤어_배율은_얼굴_적용_전에_거부한다(float depth)
        {
            var saved = AssetDatabase.LoadAssetAtPath<BuildPreset>(PresetPath);
            var profile = UnityEngine.Object.Instantiate(saved.config.FaceMeshProfile);
            try
            {
                profile.hairAttachments = new[] { new P09FaceMeshProfileSO.HairAttachment
                {
                    constraintPath = "Hair", localScale = new Vector3(depth, 1, 1)
                }};
                Assert.Throws<InvalidOperationException>(() => profile.ValidateProfile());
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 헤어_경로가_잘못되면_얼굴도_부분_적용하지_않는다()
        {
            var saved = AssetDatabase.LoadAssetAtPath<BuildPreset>(PresetPath);
            var profile = UnityEngine.Object.Instantiate(saved.config.FaceMeshProfile);
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PathConfig.BasePrefabFemaleNoPhysics));
            try
            {
                var face = root.transform.Find(profile.rendererPaths[0]).GetComponent<SkinnedMeshRenderer>();
                var before = face.sharedMesh;
                profile.hairAttachments = new[] { new P09FaceMeshProfileSO.HairAttachment { constraintPath = "Missing/Hair" } };
                Assert.Throws<InvalidOperationException>(() => profile.Apply(root));
                Assert.That(face.sharedMesh, Is.SameAs(before));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 프리셋_직렬화와_초기화가_얼굴_연결을_보존하고_해제한다()
        {
            var saved = AssetDatabase.LoadAssetAtPath<BuildPreset>(PresetPath);
            var copy = JsonUtility.FromJson<CharacterBuildConfig>(JsonUtility.ToJson(saved.config));
            Assert.That(copy.FaceMeshProfile, Is.SameAs(saved.config.FaceMeshProfile));
            copy.Sex = BuilderSex.Male;
            Assert.That(copy.Validate().Any(message => message.Contains("같은 성별")), Is.True);
            copy.Reset();
            Assert.That(copy.FaceMeshProfile, Is.Null);
        }
    }
}
#endif
