using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.Tests
{
    /// <summary>스태미나 초기 숨김, 실제 값 변화, 추적 스냅 및 물결 재질 격리를 검증합니다.</summary>
    public sealed class StaminaHudTests
    {
        private GameObject _root;
        private UI_HUD_PlayerInfo _hud;
        private UPlayGround.Data.Config.SettingsData _settings;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        [SetUp]
        public void SetUp()
        {
            _root = PrefabUtility.LoadPrefabContents("Assets/03.Prefabs/UI/HUD/UI_HUD_PlayerInfo.prefab");
            _hud = _root.GetComponent<UI_HUD_PlayerInfo>();
            _settings = ScriptableObject.CreateInstance<UPlayGround.Data.Config.SettingsData>();
            _settings.reduceHudMotion = true;
            typeof(UI_HUD_PlayerInfo).GetField("_settings", PrivateInstance).SetValue(_hud, _settings);
        }

        [TearDown]
        public void TearDown()
        {
            Material runtime = Get<Material>("_staminaWaterMaterial");
            if (runtime != null)
            {
                Get<Image>("_staminaFill").material = Get<Material>("_staminaWaterSource");
                UnityEngine.Object.DestroyImmediate(runtime);
            }
            PrefabUtility.UnloadPrefabContents(_root);
            UnityEngine.Object.DestroyImmediate(_settings);
        }

        [TestCase(100f)]
        [TestCase(35f)]
        [TestCase(0f)]
        public void 초기값과_교체값은_잔량과_무관하게_숨긴다(float stamina)
        {
            Call("SetStaminaImmediate", stamina, 100f);
            Call("RefreshStaminaVisibility", Time.unscaledTime + 10f);
            Assert.That(Get<RectTransform>("_staminaPanel").gameObject.activeSelf, Is.False);
            Assert.That(Get<CanvasGroup>("_staminaGroup").alpha, Is.Zero);
            Assert.That(Get<float>("_lastStaminaChangeTime"), Is.EqualTo(float.NegativeInfinity));
        }

        [TestCase(45f)]
        [TestCase(55f)]
        public void 소비와_회복만_활동으로_기록하고_최대값_변경은_무시한다(float changed)
        {
            Call("SetStaminaImmediate", 50f, 100f);
            _hud.SetStamina(50f, 120f);
            Assert.That(Get<float>("_lastStaminaChangeTime"), Is.EqualTo(float.NegativeInfinity));
            _hud.SetStamina(changed, 120f);
            Assert.That(Get<float>("_lastStaminaChangeTime"), Is.EqualTo(Time.unscaledTime));
            Call("SetStaminaImmediate", changed, 120f);
            Assert.That(Get<float>("_lastStaminaChangeTime"), Is.EqualTo(float.NegativeInfinity));
        }

        [Test]
        public void 회복_대기와_회복_중에는_유지하고_완전히_회복한_뒤_지연하여_숨긴다()
        {
            Call("SetStaminaImmediate", 100f, 100f);
            _hud.SetStamina(40f, 100f);
            float changedTime = Get<float>("_lastStaminaChangeTime");
            float graceTime = Get<float>("_staminaActivityGraceTime");
            float hideDelay = Get<float>("_staminaHideDelay");
            Call("RefreshStaminaVisibility", changedTime);
            Assert.That(Get<CanvasGroup>("_staminaGroup").alpha, Is.EqualTo(1f));
            Call("RefreshStaminaVisibility", changedTime + graceTime + hideDelay * .5f);
            Assert.That(Get<CanvasGroup>("_staminaGroup").alpha, Is.EqualTo(1f), "회복 대기 중에도 숨김 지연이 끝나기 전에는 유지한다");
            Call("RefreshStaminaVisibility", changedTime + graceTime + hideDelay + .01f);
            Assert.That(Get<CanvasGroup>("_staminaGroup").alpha, Is.EqualTo(1f), "회복 대기가 숨김 지연보다 길어도 표시를 유지한다");
            _hud.SetStamina(41f, 100f);
            Call("RefreshStaminaVisibility", Time.unscaledTime + graceTime + hideDelay + 10f);
            Assert.That(Get<CanvasGroup>("_staminaGroup").alpha, Is.EqualTo(1f));
            _hud.SetStamina(100f, 100f);
            changedTime = Get<float>("_lastStaminaChangeTime");
            Call("RefreshStaminaVisibility", changedTime + graceTime + hideDelay * .5f);
            Assert.That(Get<CanvasGroup>("_staminaGroup").alpha, Is.EqualTo(1f), "회복 완료 후에도 숨김 지연을 적용한다");
            Call("RefreshStaminaVisibility", changedTime + graceTime + hideDelay + .01f);
            Assert.That(Get<CanvasGroup>("_staminaGroup").alpha, Is.Zero);
        }

        [Test]
        public void 처음_표시와_순간이동은_즉시_맞추고_미세진동은_무시한다()
        {
            var panel = Get<RectTransform>("_staminaPanel");
            Call("FollowStaminaPosition", new Vector2(200f, 100f));
            Assert.That((Vector2)panel.localPosition, Is.EqualTo(new Vector2(200f, 100f)));
            Call("FollowStaminaPosition", new Vector2(200.5f, 100f));
            Assert.That((Vector2)panel.localPosition, Is.EqualTo(new Vector2(200f, 100f)));
            Call("FollowStaminaPosition", new Vector2(700f, 100f));
            Assert.That((Vector2)panel.localPosition, Is.EqualTo(new Vector2(700f, 100f)));
        }

        [Test]
        public void 물결_재질은_HUD마다_분리되고_공유_에셋을_수정하지_않는다()
        {
            var fill = Get<Image>("_staminaFill");
            Material original = fill.material;
            Assert.That(original.shader, Is.SameAs(AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/06.Shaders/UI/StaminaWaterCanvas.shadergraph")));
            Assert.That(ShaderUtil.ShaderHasError(original.shader), Is.False);
            Call("InitializeStaminaWater");
            Assert.That(fill.material, Is.Not.SameAs(original));
            fill.material.SetFloat("_UnscaledTime", 123f);
            Assert.That(original.GetFloat("_UnscaledTime"), Is.Not.EqualTo(123f));
            Call("TickStaminaWater", true);
            Assert.That(fill.material.GetFloat("_UnscaledTime"), Is.Zero);
            Call("SetStaminaImmediate", 65f, 100f);
            Assert.That(fill.material.GetFloat("_Resource"), Is.EqualTo(.65f).Within(.001f));
        }

        [Test]
        public void 곡선_게이지의_잔량별_렌더링을_기록한다()
        {
            var importer = typeof(UnityEditor.AssetImporters.ScriptedImporter).Assembly;
            System.Type graphImporter = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                graphImporter = assembly.GetType("UnityEditor.ShaderGraph.ShaderGraphImporter");
                if (graphImporter != null) break;
            }
            if (graphImporter != null)
                foreach (var method in graphImporter.GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
                    if (method.Name == "GetShaderText" && method.GetParameters().Length == 2)
                    {
                        object[] args = { "Assets/06.Shaders/UI/StaminaWaterCanvas.shadergraph", null };
                        System.IO.Directory.CreateDirectory("Logs/StaminaHud");
                        System.IO.File.WriteAllText("Logs/StaminaHud/Generated.shader", (string)method.Invoke(null, args));
                        break;
                    }
            Call("InitializeStaminaWater");
            var cameraObject = new GameObject("StaminaPreviewCamera", typeof(UnityEngine.Camera));
            var canvasObject = new GameObject("StaminaPreviewCanvas", typeof(RectTransform), typeof(Canvas));
            var camera = cameraObject.GetComponent<UnityEngine.Camera>();
            var canvas = canvasObject.GetComponent<Canvas>();
            var target = new RenderTexture(640, 240, 24);
            var capture = new Texture2D(640, 240, TextureFormat.RGB24, false);
            var previewMaterials = new System.Collections.Generic.List<Material>();
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.12f, .16f, .14f);
                camera.cullingMask = 1 << 31;
                canvasObject.layer = 31;
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                float[] ratios = { 1f, .65f, .2f, 0f };
                for (int i = 0; i < ratios.Length; i++)
                {
                    var panel = UnityEngine.Object.Instantiate(Get<RectTransform>("_staminaPanel"), canvas.transform);
                    foreach (Transform child in panel.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                    panel.gameObject.SetActive(true);
                    panel.GetComponent<CanvasGroup>().alpha = 1f;
                    panel.anchoredPosition = new Vector2(-280f + i * 160f, 0f);
                    var fill = panel.Find("StaminaFill").GetComponent<Image>();
                    var material = new Material(fill.material);
                    previewMaterials.Add(material);
                    material.SetFloat("_Resource", ratios[i]);
                    fill.material = material;
                }
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0, 0, 640, 240), 0, 0);
                capture.Apply();
                Color32[] pixels = capture.GetPixels32();
                int[] goldPixels = new int[4];
                for (int y = 0; y < 240; y++)
                    for (int x = 0; x < 640; x++)
                    {
                        Color32 pixel = pixels[y * 640 + x];
                        if (pixel.r > 150 && pixel.r > pixel.b * 1.5f) goldPixels[x / 160]++;
                    }
                System.IO.Directory.CreateDirectory("Logs/StaminaHud");
                System.IO.File.WriteAllBytes("Logs/StaminaHud/FillStates.png", capture.EncodeToPNG());
                Assert.That(goldPixels[0], Is.GreaterThan(100), "채움이 실제로 렌더링되어야 합니다.");
                Assert.That(goldPixels[0], Is.GreaterThan(goldPixels[1]));
                Assert.That(goldPixels[1], Is.GreaterThan(goldPixels[2]));
                Assert.That(goldPixels[2], Is.GreaterThan(goldPixels[3]));
                Assert.That(goldPixels[3], Is.Zero, "빈 프레임에 채움의 끝점이 남지 않아야 합니다.");
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(canvasObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                foreach (Material material in previewMaterials) UnityEngine.Object.DestroyImmediate(material);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(capture);
            }
        }

        private T Get<T>(string name) => (T)typeof(UI_HUD_PlayerInfo)
            .GetField(name, PrivateInstance).GetValue(_hud);

        private void Call(string name, params object[] arguments) => typeof(UI_HUD_PlayerInfo)
            .GetMethod(name, PrivateInstance).Invoke(_hud, arguments);
    }
}
