using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.Cinematic.Tests
{
    /// <summary>근접 디더의 재질 복구가 다른 표현 상태를 매 프레임 덮어쓰지 않는지 검사한다.</summary>
    public sealed class ActorCameraProximityDitherTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _actor;
        private GameObject _cameraObject;
        private Material _original;
        private Material _runtime;
        private Material _replacement;
        private ActorCameraProximityDither _dither;
        private Renderer _renderer;

        [SetUp]
        public void SetUp()
        {
            _actor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _actor.SetActive(false);
            _renderer = _actor.GetComponent<Renderer>();
            Shader shader = Shader.Find("lilToon");
            Assert.That(shader, Is.Not.Null);
            _original = new Material(shader);
            _runtime = new Material(shader);
            _replacement = new Material(shader);
            _renderer.sharedMaterial = _original;
            _dither = _actor.AddComponent<ActorCameraProximityDither>();
            _dither.RefreshRenderers();
            _actor.SetActive(true);

            _cameraObject = new GameObject("디더 검사용 카메라", typeof(Camera));
            _cameraObject.transform.position = Vector3.one * 1000;
            Set("_camera", _cameraObject.GetComponent<Camera>());
            Set("_runtimePrepared", true);
            Set("_lastAppliedVisibility", 1f);
            Set("_fadeSpeed", 0f);
            Set("_fullyVisibleEvaluationTimer", 1f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_actor);
            Object.DestroyImmediate(_cameraObject);
            Object.DestroyImmediate(_original);
            Object.DestroyImmediate(_runtime);
            Object.DestroyImmediate(_replacement);
        }

        [Test]
        public void 완전히_보이는_동안_다른_시스템의_렌더러_숨김을_덮어쓰지_않는다()
        {
            _renderer.forceRenderingOff = true;
            Call("LateUpdate");
            Assert.That(_renderer.forceRenderingOff, Is.True);
            Assert.That(_renderer.sharedMaterial, Is.SameAs(_original));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 페이드_종료시_한번_복구하고_외부_재질_교체를_보존한다(bool externallyReplaced)
        {
            var infos = (System.Collections.IList)typeof(ActorCameraProximityDither)
                .GetField("_rendererInfos", PrivateInstance).GetValue(_dither);
            Assert.That(infos.Count, Is.EqualTo(1));
            infos[0].GetType().GetField("RuntimeMaterials").SetValue(infos[0], new[] { _runtime });
            Call("EnsureRuntimeMaterials");
            Assert.That(_renderer.sharedMaterial, Is.SameAs(_runtime));

            if (externallyReplaced) _renderer.sharedMaterial = _replacement;
            Set("_visibility", 0.5f);
            Call("LateUpdate");
            Assert.That(_renderer.sharedMaterial, Is.SameAs(externallyReplaced ? _replacement : _original));
            Assert.That(typeof(ActorCameraProximityDither).GetField("_hasAppliedRuntimeMaterials", PrivateInstance)
                .GetValue(_dither), Is.False);

            _renderer.forceRenderingOff = true;
            Set("_fullyVisibleEvaluationTimer", 1f);
            Call("LateUpdate");
            Assert.That(_renderer.forceRenderingOff, Is.True);
        }

        private void Set(string name, object value) =>
            typeof(ActorCameraProximityDither).GetField(name, PrivateInstance).SetValue(_dither, value);

        private void Call(string name) =>
            typeof(ActorCameraProximityDither).GetMethod(name, PrivateInstance).Invoke(_dither, null);
    }
}
