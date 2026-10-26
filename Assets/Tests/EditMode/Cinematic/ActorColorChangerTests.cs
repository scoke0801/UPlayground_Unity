using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.Cinematic.Tests
{
    /// <summary>피격 전후에 불필요한 MPB가 남지 않고 기존 렌더링 속성이 보존되는지 검사한다.</summary>
    public sealed class ActorColorChangerTests
    {
        private GameObject _root;
        private Material _material;
        private Renderer _renderer;
        private ActorColorChanger _changer;

        [SetUp]
        public void SetUp()
        {
            _root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _renderer = _root.GetComponent<Renderer>();
            _material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _renderer.sharedMaterial = _material;
            _changer = _root.AddComponent<ActorColorChanger>();
            _changer.InitializeRendererData();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_material);
        }

        [Test]
        public void 초기화와_재초기화는_기본색_MPB를_만들지_않는다()
        {
            _changer.InitializeRendererData();
            _changer.RefreshRenderers();
            Assert.That(_renderer.HasPropertyBlock(), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 피격_종료는_기존_MPB를_복구하고_없었다면_제거한다(bool hasProperties)
        {
            int color = Shader.PropertyToID("_BaseColor");
            int extra = Shader.PropertyToID("_Smoothness");
            var properties = new MaterialPropertyBlock();
            if (hasProperties)
            {
                properties.SetColor(color, Color.green);
                properties.SetFloat(extra, 0.27f);
                _renderer.SetPropertyBlock(properties);
            }
            Capture();
            IEnumerator flash = CreateFlash();
            Assert.That(flash.MoveNext(), Is.True);
            Assert.That(_renderer.HasPropertyBlock(), Is.True);
            // 재피격해도 이미 적용된 빨간색을 원본으로 다시 저장하면 안 된다.
            Capture();
            Assert.That(flash.MoveNext(), Is.False);
            Assert.That(_renderer.HasPropertyBlock(), Is.EqualTo(hasProperties));
            if (hasProperties)
            {
                _renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetColor(color), Is.EqualTo(Color.green));
                Assert.That(properties.GetFloat(extra), Is.EqualTo(0.27f));
            }
        }

        [Test]
        public void 피격_중_비활성화는_색상과_MPB를_복구한다()
        {
            Capture();
            CreateFlash().MoveNext();
            _changer.enabled = false;
            typeof(ActorColorChanger).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_changer, null);
            Assert.That(_renderer.HasPropertyBlock(), Is.False);
        }

        private void Capture() => typeof(ActorColorChanger)
            .GetMethod("CaptureOriginalProperties", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(_changer, null);

        private IEnumerator CreateFlash() => (IEnumerator)typeof(ActorColorChanger)
            .GetMethod("FlashRoutine", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(_changer, null);
    }
}
