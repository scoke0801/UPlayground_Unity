using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI.Tests
{
    /// <summary>표시 교체와 비동기 종료가 다른 요청의 패널을 닫지 않는지 검사한다.</summary>
    public sealed class FaceStripPresenterTests
    {
        private GameObject _root;
        private UIFaceStrip _strip;
        private CanvasGroup _group;
        private Sprite _sprite;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("FaceStripTest", typeof(RectTransform), typeof(CanvasGroup));
            _group = _root.GetComponent<CanvasGroup>();
            _strip = _root.AddComponent<UIFaceStrip>();
            _sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
            var serialized = new SerializedObject(_strip);
            serialized.FindProperty("_group").objectReferenceValue = _group;
            serialized.FindProperty("_panel").objectReferenceValue = _root.transform;
            foreach (string field in new[] { "_background", "_face", "_flash" })
            {
                var image = new GameObject(field, typeof(RectTransform), typeof(Image));
                image.transform.SetParent(_root.transform, false);
                serialized.FindProperty(field).objectReferenceValue = image.GetComponent<Image>();
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            UnityEngine.Object.DestroyImmediate(_sprite);
        }

        private FaceStripRequest Request => new FaceStripRequest(
            _sprite, Color.blue, 0.2f, 0.5f, 0.1f, 0.05f, 0.01f, 0.1f);

        [Test]
        public void UIManager는_관리_업데이트에_등록될_계약을_구현한다()
        {
            Assert.That(typeof(IUpdatableManager).IsAssignableFrom(typeof(UIManager)), Is.True);
        }

        [Test]
        public void 교체_후_이전_토큰은_새_표시를_닫지_않는다()
        {
            long oldToken = _strip.Show(Request);
            long token = _strip.Show(Request);
            Assert.That(oldToken, Is.GreaterThan(0));
            Assert.That(token, Is.GreaterThan(oldToken));
            _strip.Hide(oldToken);
            Assert.That(_strip.IsVisible, Is.True);
            _strip.Hide(token);
            Assert.That(_strip.IsVisible, Is.False);
            Assert.That(_group.alpha, Is.Zero);
        }

        [Test]
        public void 종료한_패널은_다시_표시하지_않는다()
        {
            _strip.Show(Request);
            _strip.Dispose();
            Assert.That(_strip.Show(Request), Is.Zero);
            Assert.That(_strip.IsVisible, Is.False);
        }

        [Test]
        public void 얼굴이_없으면_기존_표시도_즉시_정리한다()
        {
            _strip.Show(Request);
            Assert.That(_strip.Show(default), Is.Zero);
            Assert.That(_strip.IsVisible, Is.False);
        }

        [Test]
        public void 재생성된_패널도_이전_토큰을_거부한다()
        {
            long previous = _strip.Show(Request);
            GameObject replacement = UnityEngine.Object.Instantiate(_root);
            try
            {
                UIFaceStrip next = replacement.GetComponent<UIFaceStrip>();
                long token = next.Show(Request);
                Assert.That(token, Is.GreaterThan(previous));
                next.Hide(previous);
                Assert.That(next.IsVisible, Is.True);
                Assert.That(replacement.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                Assert.That(replacement.GetComponent<CanvasGroup>().interactable, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(replacement);
            }
        }

        [Test]
        public void 프레임_진행은_진입_유지_퇴장_알파를_반영한다()
        {
            _strip.Show(Request);
            MethodInfo apply = typeof(UIFaceStrip).GetMethod("ApplyFrame", BindingFlags.Instance | BindingFlags.NonPublic);
            apply.Invoke(_strip, new object[] { Request, 0.2f });
            Assert.That(_group.alpha, Is.EqualTo(1f).Within(0.001f));
            apply.Invoke(_strip, new object[] { Request, 0.8f });
            Assert.That(_group.alpha, Is.Zero.Within(0.001f));
        }

        [Test]
        public void 불투명_속성색도_배경은_반투명을_유지한다()
        {
            _strip.Show(Request);
            var serialized = new SerializedObject(_strip);
            Image background = (Image)serialized.FindProperty("_background").objectReferenceValue;
            Assert.That(background.color.a, Is.EqualTo(0.55f).Within(0.001f));
            Assert.That(background.color.b, Is.EqualTo(1f));
        }

        [Test]
        public void 플래시는_유지_끝_직전에_속성색으로_표시한다()
        {
            _strip.Show(Request);
            var serialized = new SerializedObject(_strip);
            Image flash = (Image)serialized.FindProperty("_flash").objectReferenceValue;
            MethodInfo apply = typeof(UIFaceStrip).GetMethod("ApplyFrame", BindingFlags.Instance | BindingFlags.NonPublic);
            apply.Invoke(_strip, new object[] { Request, 0.2f });
            Assert.That(flash.color.a, Is.Zero);
            apply.Invoke(_strip, new object[] { Request, 0.65f });
            Assert.That(flash.color.a, Is.GreaterThan(0f));
            Assert.That(flash.color.r, Is.Zero);
            Assert.That(flash.color.b, Is.EqualTo(1f));
            apply.Invoke(_strip, new object[] { Request, 0.7f });
            Assert.That(flash.color.a, Is.Zero.Within(0.001f));
        }
    }
}
