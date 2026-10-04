using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Config;

namespace UPlayGround.UI.Tests
{
    /// <summary>카메라 옵션의 프리팹 배선·저장 복원·포커스 스크롤과 실제 화면 배치를 검증한다.</summary>
    public sealed class CameraAssistSettingsTests
    {
        private const string PrefabPath = "Assets/03.Prefabs/UI/Scene/UI_Scene_SettingMenu.prefab";

        [Test]
        public void 두_보조_옵션은_기존_스위치_순서와_독립된_참조를_가진다()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            SettingsData data = ScriptableObject.CreateInstance<SettingsData>();
            try
            {
                var page = root.GetComponentInChildren<UISettingPageGamePlay>(true);
                var serialized = new SerializedObject(page);
                var hit = (UISwitchButton)serialized.FindProperty("_hitCameraAssist").objectReferenceValue;
                var movement = (UISwitchButton)serialized.FindProperty("_movementCameraRecentering").objectReferenceValue;
                Assert.That(hit, Is.Not.Null);
                Assert.That(movement, Is.Not.Null.And.Not.SameAs(hit));
                UISwitchButton[] switches = page.GetComponentsInChildren<UISwitchButton>(true);
                Assert.That(switches.Length, Is.EqualTo(5));
                Assert.That(switches[2], Is.SameAs(hit));
                Assert.That(switches[4], Is.SameAs(movement));
                page.Bind(data);
                page.SyncUIFromData(data);
                Assert.That(hit.IsOn, Is.True);
                Assert.That(movement.IsOn, Is.False);
                var click = typeof(UISwitchButton).GetMethod("OnClickedSwitchButton",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                click.Invoke(hit, null);
                click.Invoke(movement, null);
                Assert.That(data.hitCameraAssist, Is.False);
                Assert.That(data.movementCameraRecentering, Is.True);
                Assert.That(data.aimAssist, Is.True);

                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void 이전_저장의_조준_보조_끄기와_신규_카메라_선택을_보존한다()
        {
            const string key = "GameSettings_v1";
            bool hadPrevious = PlayerPrefs.HasKey(key);
            string previous = PlayerPrefs.GetString(key);
            SettingsData data = ScriptableObject.CreateInstance<SettingsData>();
            try
            {
                PlayerPrefs.SetString(key, "{\"aimAssist\":false}");
                data.Load();
                Assert.That(data.hitCameraAssist, Is.False);
                Assert.That(data.movementCameraRecentering, Is.False);
                data.hitCameraAssist = true;
                data.movementCameraRecentering = true;
                data.Save(flushPlayerPrefs: false);
                data.ResetToDefault();
                data.Load();
                Assert.That(data.aimAssist, Is.False);
                Assert.That(data.hitCameraAssist, Is.True);
                Assert.That(data.movementCameraRecentering, Is.True);
            }
            finally
            {
                if (hadPrevious) PlayerPrefs.SetString(key, previous);
                else PlayerPrefs.DeleteKey(key);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void 게임패드_포커스로_추가_옵션과_마지막_행까지_스크롤할_수_있다()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            GameObject cameraObject = new GameObject("설정 화면 검증 카메라", typeof(Camera));
            RenderTexture texture = new RenderTexture(1920, 1080, 24);
            SettingsData data = ScriptableObject.CreateInstance<SettingsData>();
            try
            {
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, root.scene);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.scene = root.scene;
                camera.targetTexture = texture;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                Canvas canvas = root.GetComponent<Canvas>();
                root.SetActive(true);
                canvas.enabled = true;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                foreach (CanvasGroup group in root.GetComponentsInChildren<CanvasGroup>(true))
                    group.alpha = 1f;
                UISettingPageGamePlay page = root.GetComponentInChildren<UISettingPageGamePlay>(true);
                foreach (UISettingPageBase other in root.GetComponentsInChildren<UISettingPageBase>(true))
                    other.gameObject.SetActive(other == page);
                // 프리팹 미리보기에서는 TMP_Dropdown.Awake가 실행되지 않아 템플릿을 직접 숨긴다.
                foreach (TMPro.TMP_Dropdown dropdown in page.GetComponentsInChildren<TMPro.TMP_Dropdown>(true))
                    if (dropdown.template != null) dropdown.template.gameObject.SetActive(false);
                page.SyncUIFromData(data);
                ScrollRect scroll = page.GetComponent<ScrollRect>();
                Assert.That(scroll, Is.Not.Null);
                Assert.That(scroll.horizontal, Is.False);
                Assert.That(scroll.viewport.GetComponent<RectMask2D>(), Is.Not.Null);
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                Canvas.ForceUpdateCanvases();
                Selectable[] controls = page.GetComponentsInChildren<Selectable>();
                UIFocusNavigation.ConfigureVertical(controls);
                Assert.That(controls.Length, Is.EqualTo(10));
                Assert.That(controls[0].navigation.selectOnDown, Is.Not.Null);
                Assert.That(controls[controls.Length - 1].navigation.selectOnUp, Is.Not.Null);
                Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
                scroll.verticalNormalizedPosition = 1f;
                SavePreview(camera, texture, "settings-top.png");
                RectTransform last = (RectTransform)controls[controls.Length - 1].transform;
                scroll.normalizedPosition = UIFocusScope.CalculateNormalizedPositionFor(scroll, last, 8f);
                Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4];
                last.GetWorldCorners(corners);
                Assert.That(scroll.viewport.InverseTransformPoint(corners[0]).y,
                    Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin - 1f));
                Assert.That(scroll.viewport.InverseTransformPoint(corners[2]).y,
                    Is.LessThanOrEqualTo(scroll.viewport.rect.yMax + 1f));
                SavePreview(camera, texture, "settings-bottom.png");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(data);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SavePreview(Camera camera, RenderTexture texture, string filename)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            var pixels = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("Logs/CameraAssist");
                File.WriteAllBytes(Path.Combine("Logs/CameraAssist", filename), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
