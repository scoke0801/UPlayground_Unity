using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Config;

namespace UPlayGround.Cinematic.Tests
{
    /// <summary>외곽선 LOD의 패스 감소·원본 보존과 디더/대화 복구를 검사한다.</summary>
    public sealed class ActorOutlineLodTests
    {
        private GameObject _root;
        private Renderer _renderer;
        private Material _source;
        private Material _external;
        private NpcOutlineLodSettingsSO _settings;
        private ActorOutlineLod _lod;

        [SetUp]
        public void SetUp()
        {
            _root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _root.SetActive(false);
            _renderer = _root.GetComponent<Renderer>();
            _settings = ScriptableObject.CreateInstance<NpcOutlineLodSettingsSO>();
            _settings.opaqueShader = Shader.Find("lilToon");
            _settings.cutoutShader = Shader.Find("Hidden/lilToonCutout");
            _source = new Material(Shader.Find("Hidden/lilToonOutline"));
            _external = new Material(_settings.opaqueShader);
            _renderer.sharedMaterial = _source;
            _lod = new ActorOutlineLod();
            _lod.AddRenderer(_renderer, _renderer.sharedMaterials);
        }

        [TearDown]
        public void TearDown()
        {
            _lod.Dispose();
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_source);
            Object.DestroyImmediate(_external);
            Object.DestroyImmediate(_settings);
        }

        [TestCase("Hidden/lilToonOutline", "lilToon")]
        [TestCase("Hidden/lilToonCutoutOutline", "Hidden/lilToonCutout")]
        public void 외곽선_패스만_줄이고_표면_속성과_그림자_패스를_유지한다(string original, string reduced)
        {
            _source.shader = Shader.Find(original);
            _source.SetColor("_Color", Color.green);
            _source.SetFloat("_Cutoff", 0.37f);
            _source.renderQueue = 2473;
            _renderer.forceRenderingOff = true;
            _lod.Evaluate(_settings, 10000f, 60f);
            Material actual = _renderer.sharedMaterial;
            Assert.That(actual.shader.name, Is.EqualTo(reduced));
            Assert.That(actual.passCount, Is.LessThan(_source.passCount));
            Assert.That(actual.FindPass("SHADOW_CASTER"), Is.GreaterThanOrEqualTo(0));
            Assert.That(actual.FindPass("DEPTHONLY"), Is.GreaterThanOrEqualTo(0));
            Assert.That(actual.GetColor("_Color"), Is.EqualTo(Color.green));
            Assert.That(actual.GetFloat("_Cutoff"), Is.EqualTo(0.37f));
            Assert.That(actual.renderQueue, Is.EqualTo(2473));
            Assert.That(_source.shader.name, Is.EqualTo(original));
            Assert.That(_renderer.forceRenderingOff, Is.True);
            _lod.Restore();
            Assert.That(_renderer.sharedMaterial, Is.SameAs(_source));
        }

        [Test]
        public void 거리_히스테리시스와_줌은_반복전환과_확대화면의_품질저하를_막는다()
        {
            _lod.Evaluate(_settings, 36f * 36f, 60f);
            Assert.That(_lod.IsReduced, Is.True);
            Material reduced = _renderer.sharedMaterial;
            _lod.Evaluate(_settings, 32f * 32f, 60f);
            Assert.That(_renderer.sharedMaterial, Is.SameAs(reduced));
            _lod.Evaluate(_settings, 29f * 29f, 60f);
            Assert.That(_renderer.sharedMaterial, Is.SameAs(_source));
            _lod.Evaluate(_settings, 32f * 32f, 60f);
            Assert.That(_lod.IsReduced, Is.False);
            _lod.Evaluate(_settings, 36f * 36f, 60f);
            Assert.That(_renderer.sharedMaterial, Is.SameAs(reduced));
            _lod.Evaluate(_settings, 36f * 36f, 20f);
            Assert.That(_lod.IsReduced, Is.False);
        }

        [Test]
        public void 외부가_일부슬롯을_교체해도_나머지_소유슬롯을_복구한다()
        {
            _lod.Dispose();
            _renderer.sharedMaterials = new[] { _source, _source };
            _lod.AddRenderer(_renderer, _renderer.sharedMaterials);
            _lod.Evaluate(_settings, 10000f, 60f);
            Material reduced = _renderer.sharedMaterial;
            _renderer.sharedMaterials = new[] { _external, reduced };
            _lod.Restore();
            Assert.That(_renderer.sharedMaterials, Is.EqualTo(new[] { _external, _source }));
            _lod.Evaluate(_settings, 10000f, 60f);
            Assert.That(_renderer.sharedMaterials, Is.EqualTo(new[] { _external, _source }));
        }

        [TestCase("Hidden/lilToonTransparentOutline")]
        [TestCase("Hidden/lilToonTwoPassTransparentOutline")]
        [TestCase("_lil/lilToonMulti")]
        public void 지원하지_않는_표면은_원본을_유지한다(string shader)
        {
            _source.shader = Shader.Find(shader);
            Assert.That(_source.shader, Is.Not.Null);
            _lod.Evaluate(_settings, 10000f, 60f);
            Assert.That(_renderer.sharedMaterial, Is.SameAs(_source));
        }

        [Test]
        public void 설정해제와_파괴는_원본을_복구하고_임시재질을_해제한다()
        {
            _lod.Evaluate(_settings, 10000f, 60f);
            Material reduced = _renderer.sharedMaterial;
            _settings.enableOutlineLod = false;
            _lod.Evaluate(_settings, 10000f, 60f);
            Assert.That(_renderer.sharedMaterial, Is.SameAs(_source));
            _settings.enableOutlineLod = true;
            _lod.Evaluate(_settings, 10000f, 60f);
            _lod.Dispose();
            Assert.That(_renderer.sharedMaterial, Is.SameAs(_source));
            Assert.That(reduced == null, Is.True);
        }

        [TestCase("UpdateSuppression")]
        [TestCase("EnsureRuntimeMaterials")]
        [TestCase("OnDisable")]
        public void 대화억제_근접디더_비활성화가_원본을_먼저_복구한다(string transition)
        {
            var dither = _root.AddComponent<ActorCameraProximityDither>();
            dither.RefreshRenderers();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var ownerLod = (ActorOutlineLod)typeof(ActorCameraProximityDither).GetField("_outlineLod", flags).GetValue(dither);
            ownerLod.AddRenderer(_renderer, _renderer.sharedMaterials);
            ownerLod.Evaluate(_settings, 10000f, 60f);
            Assert.That(_renderer.sharedMaterial, Is.Not.SameAs(_source));
            if (transition == "UpdateSuppression") dither.BeginExternalSuppression();
            typeof(ActorCameraProximityDither).GetMethod(transition, flags).Invoke(dither, null);
            Assert.That(_renderer.sharedMaterial, Is.SameAs(_source));
            ownerLod.Dispose();
        }
    }
}
