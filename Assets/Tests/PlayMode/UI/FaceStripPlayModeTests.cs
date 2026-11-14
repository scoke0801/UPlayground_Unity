#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI.PlayModeTests
{
    /// <summary>실제 얼굴 패널 프리팹의 실시간 수명과 HUD 독립 표시를 검사한다.</summary>
    public sealed class FaceStripPlayModeTests
    {
        private const string PrefabPath = "Assets/03.Prefabs/UI/Common/UIFaceStrip.prefab";
        private const string FacePath = "Assets/04.Images/SkillCutIn/CutIn_Raon.png";
        private const string ScreenshotPath = "output/weapon-ultimate-presentation/face-strip-playmode.png";
        private float _originalTimeScale;
        private GameObject _root;
        private GameObject _hud;
        private UIFaceStrip _strip;

        [SetUp]
        public void SetUp() => _originalTimeScale = Time.timeScale;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = _originalTimeScale;
            // 테스트가 만든 패널의 트윈만 정리해 다른 실행 중 연출을 보존한다.
            if (_strip != null)
                _strip.Dispose();
            if (_root != null)
                UnityEngine.Object.Destroy(_root);
            if (_hud != null)
                UnityEngine.Object.Destroy(_hud);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 느린_전투_시간에도_실시간으로_표시하고_자연_종료한다()
        {
            Sprite face = CreatePanel();
            Time.timeScale = 0.2f;
            long token = _strip.Show(new FaceStripRequest(
                face, new Color(0.35f, 0.65f, 1f), 0.18f, 0.3f, 0.15f, 0.04f, 0.01f, 0.08f));
            Assert.That(token, Is.GreaterThan(0));

            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(_strip.IsVisible, Is.True);
            Assert.That(_root.GetComponent<CanvasGroup>().alpha, Is.GreaterThan(0f));
            Directory.CreateDirectory(Path.GetDirectoryName(ScreenshotPath));
            ScreenCapture.CaptureScreenshot(ScreenshotPath);

            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(_strip.IsVisible, Is.False);
            Assert.That(_root.GetComponent<CanvasGroup>().alpha, Is.Zero);
        }

        [UnityTest]
        public IEnumerator HUD가_꺼져도_독립_캔버스는_표시하고_토큰으로_닫힌다()
        {
            _hud = new GameObject("DisabledHudTest", typeof(RectTransform), typeof(Canvas));
            _hud.SetActive(false);
            Sprite face = CreatePanel();
            Canvas canvas = _root.GetComponent<Canvas>();
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(_root.transform.parent, Is.Null);
            Assert.That(_root.GetComponentsInChildren<GraphicRaycaster>(true), Is.Empty);
            Assert.That(_root.GetComponentsInChildren<Selectable>(true), Is.Empty);
            Assert.That(_root.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            Assert.That(_root.GetComponent<CanvasGroup>().interactable, Is.False);

            long token = _strip.Show(new FaceStripRequest(
                face, Color.cyan, 0.08f, 0.5f, 0.1f, 0.02f, 0.01f, 0.08f));
            Assert.That(token, Is.GreaterThan(0));
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(_hud.activeSelf, Is.False);
            Assert.That(_strip.IsVisible, Is.True);
            Assert.That(_root.GetComponent<CanvasGroup>().alpha, Is.GreaterThan(0f));

            _root.SetActive(false);
            Assert.That(_strip.IsVisible, Is.False);
            _root.SetActive(true);
            Assert.That(_strip.IsVisible, Is.False);
            token = _strip.Show(new FaceStripRequest(face, Color.cyan, 0.08f, 0.5f, 0.1f, 0.02f, 0.01f, 0.08f));
            _strip.Hide(token);
            Assert.That(_strip.IsVisible, Is.False);
            Assert.That(_root.GetComponent<CanvasGroup>().alpha, Is.Zero);
        }

        private Sprite CreatePanel()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Sprite face = AssetDatabase.LoadAssetAtPath<Sprite>(FacePath);
            Assert.That(prefab, Is.Not.Null, $"실제 얼굴 패널 프리팹 필요: {PrefabPath}");
            Assert.That(face, Is.Not.Null, $"실제 라온 얼굴 스프라이트 필요: {FacePath}");
            _root = UnityEngine.Object.Instantiate(prefab);
            _strip = _root.GetComponent<UIFaceStrip>();
            Assert.That(_strip, Is.Not.Null);
            return face;
        }
    }
}
#endif
