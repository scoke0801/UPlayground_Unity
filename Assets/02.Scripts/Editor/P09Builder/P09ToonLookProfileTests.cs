#if UNITY_INCLUDE_TESTS
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder.Tests
{
    /// <summary>얼굴 분리와 반복 적용, 잘못된 프로파일의 부분 적용 방지를 검사한다.</summary>
    public sealed class P09ToonLookProfileTests
    {
        private const string ProfilePath = "Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Daon_ToonLook.asset";

        [Test]
        public void 얼굴_조건은_공용_피부보다_우선하고_눈은_보존한다()
        {
            var profile = AssetDatabase.LoadAssetAtPath<P09ToonLookProfileSO>(ProfilePath);
            Assert.That(profile, Is.Not.Null);
            profile.ValidateBindings();
            Material source = profile.bindings[0].source;
            Assert.That(profile.Resolve("Female_Face_02", source), Is.SameAs(profile.bindings[1].replacement));
            Assert.That(profile.Resolve("Fem_Armor_005_Chest", source), Is.SameAs(profile.bindings[0].replacement));
            var eye = AssetDatabase.LoadAssetAtPath<Material>("Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Materials/Common/Eye/P09_Eye_Yellow.mat");
            Assert.That(eye, Is.Not.Null);
            Assert.That(profile.Resolve("Female_Face_02", eye), Is.SameAs(eye));
        }

        [Test]
        public void 반복_적용은_슬롯과_텍스처를_바꾸지_않는다()
        {
            var profile = AssetDatabase.LoadAssetAtPath<P09ToonLookProfileSO>(ProfilePath);
            var root = new GameObject("Female_Face_02");
            try
            {
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = new[] { profile.bindings[0].source, null };
                Assert.That(profile.Apply(root), Is.EqualTo(1));
                Assert.That(profile.Apply(root), Is.Zero);
                Assert.That(renderer.sharedMaterials.Length, Is.EqualTo(2));
                Assert.That(renderer.sharedMaterials[1], Is.Null);
                foreach (var binding in profile.bindings)
                    foreach (string property in binding.source.GetTexturePropertyNames())
                    {
                        var original = binding.source.GetTexture(property);
                        var replacement = binding.replacement.GetTexture(property);
                        if (replacement == original) continue;
                        Assert.That(binding.rendererPrefix, Is.EqualTo("Female_Face_"));
                        Assert.That(original, Is.SameAs(binding.source.GetTexture("_MainTex")), property);
                        string path = AssetDatabase.GetAssetPath(replacement);
                        StringAssert.StartsWith("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/Textures/", path);
                        using (var hash = System.Security.Cryptography.SHA256.Create())
                            CollectionAssert.AreEqual(hash.ComputeHash(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(original))), hash.ComputeHash(System.IO.File.ReadAllBytes(path)));
                        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                        Assert.That(importer.GetPlatformTextureSettings("Standalone").maxTextureSize, Is.EqualTo(4096));
                    }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void 이미_교체된_공용_피부도_얼굴_조건을_다시_평가한다()
        {
            var profile = AssetDatabase.LoadAssetAtPath<P09ToonLookProfileSO>(ProfilePath);
            var body = profile.bindings[0].replacement;
            var face = profile.bindings[1].replacement;
            Assert.That(profile.Resolve("Female_Face_02", body), Is.SameAs(face));
            Assert.That(profile.Resolve("Body", face), Is.SameAs(body));
            Assert.That(profile.Resolve("Female_Face_02", face), Is.SameAs(face));
        }

        [Test]
        public void 교체_재질의_원본이_모호하면_적용을_거부한다()
        {
            var saved = AssetDatabase.LoadAssetAtPath<P09ToonLookProfileSO>(ProfilePath);
            var profile = ScriptableObject.CreateInstance<P09ToonLookProfileSO>();
            try
            {
                profile.bindings = new[] { saved.bindings[0], new P09ToonLookProfileSO.MaterialBinding
                {
                    source = saved.bindings[2].source,
                    replacement = saved.bindings[0].replacement
                } };
                Assert.Throws<InvalidOperationException>(() => profile.ValidateBindings());
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 잘못된_후속_연결이_있으면_먼저_전체_적용을_거부한다()
        {
            var saved = AssetDatabase.LoadAssetAtPath<P09ToonLookProfileSO>(ProfilePath);
            var profile = ScriptableObject.CreateInstance<P09ToonLookProfileSO>();
            var root = new GameObject("Body");
            try
            {
                profile.bindings = new[] { saved.bindings[0], new P09ToonLookProfileSO.MaterialBinding() };
                var config = new CharacterBuildConfig { ToonLookProfile = profile };
                StringAssert.Contains("머티리얼이 누락", new System.Collections.Generic.List<string>(config.Validate())[0]);
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = saved.bindings[0].source;
                Assert.Throws<InvalidOperationException>(() => profile.Apply(root));
                Assert.That(renderer.sharedMaterial, Is.SameAs(saved.bindings[0].source));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
#endif
