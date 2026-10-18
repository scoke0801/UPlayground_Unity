using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Dialogue;

namespace UPlayGround.UI.Tests
{
    /// <summary>머리카락 뿌리 고정과 연속 변형, 정지 복원을 검증한다.</summary>
    public sealed class DialoguePortraitBendTests
    {
        private GameObject _object;
        private Texture2D _texture;
        private Sprite _sprite;
        private UIDialoguePortraitPartImage _image;

        [SetUp]
        public void SetUp()
        {
            _object = new GameObject("PortraitBendTest", typeof(RectTransform), typeof(CanvasRenderer));
            _image = _object.AddComponent<UIDialoguePortraitPartImage>();
            _image.rectTransform.sizeDelta = new Vector2(80, 240);
            _texture = new Texture2D(80, 240);
            _sprite = Sprite.Create(_texture, new Rect(0, 0, 80, 240), Vector2.one * 0.5f);
            _image.sprite = _sprite;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_object);
            UnityEngine.Object.DestroyImmediate(_sprite);
            UnityEngine.Object.DestroyImmediate(_texture);
        }

        [TestCase(8f)]
        [TestCase(-8f)]
        public void 위쪽_뿌리는_고정되고_끝까지_연속적으로_변형된다(float bend)
        {
            _image.SetBend(new Vector2(bend, 0), 0.3f);
            using var mesh = Populate();
            Assert.That(mesh.currentVertCount, Is.EqualTo(325));
            float previous = float.PositiveInfinity;
            var vertex = new UIVertex();
            for (int row = 0; row <= 24; row++)
            {
                mesh.PopulateUIVertex(ref vertex, row * 13);
                float displacement = vertex.position.x + 40;
                float magnitude = Mathf.Abs(displacement);
                Assert.That(vertex.position.y, Is.EqualTo(-120 + row * 10).Within(0.001f));
                Assert.That(magnitude, Is.LessThanOrEqualTo(previous + 0.001f));
                if (row >= 17) Assert.That(displacement, Is.EqualTo(0).Within(0.001f));
                if (row == 0) Assert.That(displacement, Is.EqualTo(bend).Within(0.001f));
                if (row == 12) Assert.That(magnitude, Is.InRange(0.1f, Mathf.Abs(bend) - 0.1f));
                previous = magnitude;
            }
        }

        [Test]
        public void 얼굴쪽_경계는_끝에서도_움직이지_않는다()
        {
            _image.SetBend(new Vector2(8, 0), 0.3f, PortraitPinnedEdge.Right, 0.3f);
            using var mesh = Populate();
            var vertex = new UIVertex();
            mesh.PopulateUIVertex(ref vertex, 12);
            Assert.That(vertex.position.x, Is.EqualTo(40).Within(0.001f));
            mesh.PopulateUIVertex(ref vertex, 9);
            Assert.That(vertex.position.x, Is.EqualTo(20).Within(0.001f));
            mesh.PopulateUIVertex(ref vertex, 0);
            Assert.That(vertex.position.x, Is.EqualTo(-32).Within(0.001f));
        }

        [Test]
        public void 변위_해제는_원래_정지_사각형으로_돌아간다()
        {
            _image.SetBend(new Vector2(8, 0), 0.3f);
            using (var bent = Populate()) Assert.That(bent.currentVertCount, Is.EqualTo(325));
            _image.SetBend(Vector2.zero, 0.3f);
            using var flat = Populate();
            Assert.That(flat.currentVertCount, Is.EqualTo(4));
            var vertex = new UIVertex();
            for (int i = 0; i < flat.currentVertCount; i++)
            {
                flat.PopulateUIVertex(ref vertex, i);
                Assert.That(Mathf.Abs(vertex.position.x), Is.EqualTo(40).Within(0.001f));
            }
        }

        private VertexHelper Populate()
        {
            var vertices = new VertexHelper();
            typeof(UIDialoguePortraitPartImage).GetMethod("OnPopulateMesh",
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(VertexHelper) }, null).Invoke(_image, new object[] { vertices });
            return vertices;
        }
    }
}
