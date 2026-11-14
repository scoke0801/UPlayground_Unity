using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>고정 외형 정리가 활성화 곡선·본 참조·교체 가능한 부품을 보존하는지 검사한다.</summary>
    public sealed class NpcAppearanceCompactionTests
    {
        private GameObject _root;
        private GameObject _parent;
        private GameObject _leaf;
        private AnimationClip _clip;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("검사용 고정 외형");
            _parent = new GameObject("부모");
            _parent.transform.SetParent(_root.transform, false);
            _leaf = new GameObject("비활성 메시", typeof(SkinnedMeshRenderer));
            _leaf.transform.SetParent(_parent.transform, false);
            _leaf.SetActive(false);
            _clip = new AnimationClip();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            UnityEngine.Object.DestroyImmediate(_clip);
        }

        [Test]
        public void 참조가_없는_비활성_메시_잎만_후보로_고른다()
        {
            Assert.That(NpcAppearancePrototype.FindUnusedMeshLeaves(_root, Array.Empty<AnimationClip>()), Is.EqualTo(new[] { _leaf.transform }));
            Assert.That(_leaf != null, Is.True);
            _leaf.SetActive(true);
            Assert.That(NpcAppearancePrototype.FindUnusedMeshLeaves(_root, Array.Empty<AnimationClip>()), Is.Empty);
        }

        [Test]
        public void 활성_렌더러의_본이_가리키는_비활성_잎은_보존한다()
        {
            var skin = _root.AddComponent<SkinnedMeshRenderer>();
            skin.bones = new[] { _leaf.transform };
            skin.rootBone = _leaf.transform;
            Assert.That(NpcAppearancePrototype.FindUnusedMeshLeaves(_root, Array.Empty<AnimationClip>()), Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 자신이나_부모의_활성화_곡선이_있으면_잎을_보존한다(bool parentBinding)
        {
            string path = AnimationUtility.CalculateTransformPath(parentBinding ? _parent.transform : _leaf.transform, _root.transform);
            AnimationUtility.SetEditorCurve(_clip, EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive"), AnimationCurve.Linear(0, 0, 1, 1));
            Assert.That(NpcAppearancePrototype.FindUnusedMeshLeaves(_root, new[] { _clip }), Is.Empty);
        }

        [Test]
        public void 런타임_외형_교체와_비활성_루트는_정리하지_않는다()
        {
            var visuals = _root.AddComponent<CharacterEquipmentVisuals>();
            Assert.Throws<InvalidOperationException>(() => NpcAppearancePrototype.FindUnusedMeshLeaves(_root, Array.Empty<AnimationClip>()));
            UnityEngine.Object.DestroyImmediate(visuals);
            _root.SetActive(false);
            Assert.Throws<InvalidOperationException>(() => NpcAppearancePrototype.FindUnusedMeshLeaves(_root, Array.Empty<AnimationClip>()));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void 잘못된_정규_입력은_백업이나_계획을_만들기_전에_거부한다(int invalidKind)
        {
            string folder = "Logs/StartupProfiling/NpcCompaction/__Test_" + Guid.NewGuid().ToString("N");
            string[] paths = invalidKind == 0 ? Array.Empty<string>() : invalidKind == 1
                ? new[] { "Assets/ExternalAssets/보호할원본.prefab" }
                : new[] { "중복", "중복" };
            Assert.Throws<ArgumentException>(() => NpcAppearanceCompactionTransaction.Prepare(paths, folder));
            Assert.That(Directory.Exists(folder), Is.False);
        }
    }
}
