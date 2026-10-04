using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UPlayGround.Components;

namespace UPlayGround.Rendering.PlayModeTests
{
    /// <summary>카메라 근접 페이드의 거리·재질 수명과 실제 GPU 디더 커버리지를 검증한다.</summary>
    public sealed class ActorCameraProximityDitherTests
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<UnityEngine.Object> _objects = new();
        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            _camera = Track(new GameObject("디더 검증 카메라")).AddComponent<Camera>();
            _camera.tag = "MainCamera";
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            _camera.nearClipPlane = 0.03f;
            _camera.orthographic = true;
            _camera.orthographicSize = 0.5f;
            _camera.transform.position = new Vector3(0f, 0f, -2f);
            _camera.cullingMask = 1 << 29;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                    UnityEngine.Object.DestroyImmediate(_objects[i]);
            }
            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator 불투명_재질은_텍스처_알파와_무관하게_같은_점_밀도를_유지한다()
        {
            Material source = CreateSource("lilToon", 0.2f);
            Material runtime = CreateRuntime(source);
            Assert.That(runtime.GetFloat("_AlphaMaskMode"), Is.EqualTo(1f));
            runtime.SetFloat("_AlphaMaskValue", 0.5f);
            CreateQuad(runtime);
            yield return null;
            Assert.That(CaptureCoverage("OpaqueHalf"), Is.EqualTo(0.5f).Within(0.025f));
            runtime.SetFloat("_AlphaMaskValue", 0f);
            Assert.That(CaptureCoverage("OpaqueHidden"), Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator 투명_재질은_원래_알파를_보존한다()
        {
            Material source = CreateSource("Hidden/lilToonTransparent", 0.2f);
            Material runtime = CreateRuntime(source);
            Assert.That(runtime.GetFloat("_AlphaMaskMode"), Is.EqualTo(2f));
            runtime.SetFloat("_AlphaMaskValue", 0.5f);
            CreateQuad(runtime);
            yield return null;
            Assert.That(CaptureCoverage("TransparentHalf"), Is.EqualTo(0.1f).Within(0.025f));
        }

        [UnityTest]
        public IEnumerator 멀티_불투명_재질도_동일한_커버리지로_사라진다()
        {
            Material source = CreateSource("_lil/lilToonMulti", 0.2f);
            source.SetFloat("_TransparentMode", 0f);
            Material runtime = CreateRuntime(source);
            Assert.That(runtime.IsKeywordEnabled("UNITY_UI_ALPHACLIP"), Is.True);
            runtime.SetFloat("_AlphaMaskValue", 0.5f);
            CreateQuad(runtime);
            yield return null;
            Assert.That(CaptureCoverage("MultiOpaqueHalf"), Is.EqualTo(0.5f).Within(0.025f));
        }

        [UnityTest]
        public IEnumerator 무기_바운드가_카메라에_닿아도_몸에서_멀면_페이드하지_않는다()
        {
            var actor = Track(new GameObject("거리 검증 액터"));
            CapsuleCollider capsule = actor.AddComponent<CapsuleCollider>();
            capsule.radius = 0.3f;
            capsule.height = 2f;
            Renderer renderer = CreateQuad(CreateSource("lilToon", 1f));
            renderer.transform.SetParent(actor.transform);
            renderer.transform.localScale = new Vector3(10f, 10f, 10f);
            renderer.transform.localPosition = _camera.transform.position;
            var dither = actor.AddComponent<ActorCameraProximityDither>();
            Set(dither, "_fadeSpeed", 0f);
            yield return null;
            yield return null;
            object[] args = { _camera.transform.position, 0f, Vector3.zero };
            Assert.That(Invoke(dither, "TryGetFadeDistance", args), Is.True);
            Assert.That((float)args[1], Is.EqualTo(1.67f).Within(0.001f));
            Assert.That(Get<float>(dither, "_visibility"), Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator 접근_후_복귀와_외부_숨김이_원본_재질을_보존한다()
        {
            var actor = Track(new GameObject("복원 검증 액터"));
            actor.AddComponent<CapsuleCollider>().radius = 0.3f;
            var presentation = actor.AddComponent<ActorPresentation>();
            Material original = CreateSource("lilToon", 1f);
            Renderer renderer = CreateQuad(original);
            renderer.transform.SetParent(actor.transform);
            var dither = actor.AddComponent<ActorCameraProximityDither>();
            Set(dither, "_fadeSpeed", 0f);
            _camera.transform.position = new Vector3(0f, 0f, -0.35f);
            yield return null;
            yield return null;
            Assert.That(Get<float>(dither, "_visibility"), Is.Zero);
            Assert.That(renderer.sharedMaterial, Is.Not.SameAs(original));
            presentation.Hide();
            _camera.transform.position = new Vector3(0f, 0f, -2f);
            yield return null;
            Assert.That(renderer.sharedMaterial, Is.SameAs(original));
            Assert.That(renderer.forceRenderingOff, Is.True);
            presentation.Show();
            Assert.That(renderer.forceRenderingOff, Is.False);

            _camera.transform.position = new Vector3(0f, 0f, -0.35f);
            Set(dither, "_fullyVisibleEvaluationTimer", 1f);
            yield return null;
            Material replacement = CreateSource("lilToon", 0.8f);
            renderer.sharedMaterial = replacement;
            dither.RestoreOriginalMaterialsImmediately();
            Assert.That(renderer.sharedMaterial, Is.SameAs(replacement));
        }

        [UnityTest]
        public IEnumerator 거리_곡선은_완전_투명에서_불투명까지_단조롭게_연결된다()
        {
            var dither = Track(new GameObject("곡선 검증 액터")).AddComponent<ActorCameraProximityDither>();
            Assert.That((float)Invoke(dither, "EvaluateTargetVisibility", -0.1f), Is.Zero);
            Assert.That((float)Invoke(dither, "EvaluateTargetVisibility", 0.12f), Is.Zero);
            Assert.That((float)Invoke(dither, "EvaluateTargetVisibility", 0.65f), Is.EqualTo(1f));
            float previous = 0f;
            for (int i = 0; i <= 100; i++)
            {
                float visibility = (float)Invoke(dither, "EvaluateTargetVisibility", i * 0.01f);
                Assert.That(visibility, Is.InRange(previous, 1f));
                previous = visibility;
            }
            yield return null;
        }

        private Material CreateSource(string shaderName, float alpha)
        {
            Shader shader = Shader.Find(shaderName);
            Assert.That(shader, Is.Not.Null, shaderName);
            var source = Track(new Material(shader));
            var texture = Track(new Texture2D(1, 1, TextureFormat.RGBA32, false, true));
            texture.SetPixel(0, 0, new Color(1f, 1f, 1f, alpha));
            texture.Apply();
            source.SetTexture("_MainTex", texture);
            source.SetColor("_Color", Color.white);
            source.SetFloat("_AsUnlit", 1f);
            source.SetFloat("_AlphaMaskMode", 0f);
            return source;
        }

        private Material CreateRuntime(Material source)
        {
            var noise = (Texture2D)typeof(ActorCameraProximityDither).GetMethod("GetDitherTexture",
                BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { 1 });
            Assert.That(noise, Is.Not.Null);
            object info = typeof(ActorCameraProximityDither).GetMethod("CreateDitherMaterial",
                BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { source, noise });
            Assert.That(info, Is.Not.Null);
            return Track((Material)info.GetType().GetField("Material").GetValue(info));
        }

        [Test]
        public void 생성_노이즈의_밝기_편향과_동률에도_페이드_분포가_균등하다()
        {
            var source = new Color32[256 * 256];
            for (int i = 0; i < source.Length; i++)
                source[i] = new Color32(128, 128, 128, 255);
            MethodInfo build = typeof(ActorCameraProximityDither).GetMethod("BuildDitherThresholds",
                BindingFlags.NonPublic | BindingFlags.Static);
            var thresholds = (byte[])build.Invoke(null, new object[] { source });
            var repeat = (byte[])build.Invoke(null, new object[] { source });
            var counts = new int[256];
            foreach (byte value in thresholds)
                counts[value]++;
            foreach (int count in counts)
                Assert.That(count, Is.EqualTo(256));
            CollectionAssert.AreEqual(thresholds, repeat);
            int firstRowVisible = 0;
            for (int i = 0; i < 256; i++)
            {
                if (thresholds[i] < 128)
                    firstRowVisible++;
            }
            Assert.That(firstRowVisible, Is.InRange(80, 176), "동률이 가로 띠로 배치되면 안 된다.");
        }

        private Renderer CreateQuad(Material material)
        {
            GameObject quad = Track(GameObject.CreatePrimitive(PrimitiveType.Quad));
            quad.layer = 29;
            Renderer renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private float CaptureCoverage(string name)
        {
            RenderTexture target = RenderTexture.GetTemporary(256, 256, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(256, 256, TextureFormat.RGB24, false);
            try
            {
                RenderPipeline.SubmitRenderRequest(_camera,
                    new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Logs/Dither");
                File.WriteAllBytes("Logs/Dither/" + name + ".png", image.EncodeToPNG());
                Color32[] pixels = image.GetPixels32();
                int covered = 0;
                for (int y = 64; y < 192; y++)
                for (int x = 64; x < 192; x++)
                {
                    Color32 pixel = pixels[y * 256 + x];
                    Assert.That(Mathf.Abs(pixel.r - pixel.g), Is.LessThan(4), "셰이더 오류 색상");
                    if (pixel.r > 128)
                        covered++;
                }
                return covered / (128f * 128f);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private T Track<T>(T item) where T : UnityEngine.Object
        {
            _objects.Add(item);
            return item;
        }

        private static object Invoke(object target, string name, params object[] args)
            => target.GetType().GetMethod(name, InstanceFlags).Invoke(target, args);

        private static T Get<T>(object target, string name)
            => (T)target.GetType().GetField(name, InstanceFlags).GetValue(target);

        private static void Set(object target, string name, object value)
            => target.GetType().GetField(name, InstanceFlags).SetValue(target, value);
    }
}
