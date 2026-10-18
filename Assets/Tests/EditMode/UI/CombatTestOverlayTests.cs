using System.Reflection;
using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TestTools;
using UPlayGround.Data.UI;
using UPlayGround.UI.Guide;

namespace UPlayGround.UI.Tests
{
    /// <summary>시험장 진단 UI의 입력 비차단과 씬별 가이드 차단 해제를 검사한다.</summary>
    public sealed class CombatTestOverlayTests
    {
        [Test]
        public void FPS_프리팹은_항상_위에_표시하고_입력을_받지_않는다()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/UI/HUD/UIFrameRateDisplay.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<UIFrameRateDisplay>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(prefab.GetComponent<Canvas>().sortingOrder, Is.EqualTo(32760));
            Assert.That(prefab.GetComponentsInChildren<GraphicRaycaster>(), Is.Empty);
            foreach (var graphic in prefab.GetComponentsInChildren<Graphic>())
                Assert.That(graphic.raycastTarget, Is.False);
            Assert.That(prefab.GetComponentInChildren<TMP_Text>().font, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator 가이드_차단은_활성_씬에만_적용되고_해제된다()
        {
            yield return new EnterPlayMode();
            var previous = SceneManager.GetActiveScene();
            var normalScene = SceneManager.CreateScene("GuideNormalTest");
            var testScene = SceneManager.CreateScene("GuideSuppressionTest");
            var owner = new GameObject("GuideSuppression");
            SceneManager.MoveGameObjectToScene(owner, testScene);
            var marker = owner.AddComponent<UIGuidePopupSuppression>();
            var data = ScriptableObject.CreateInstance<GuidePopupDataSO>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                typeof(UIGuidePopupSuppression).GetMethod("OnEnable", flags).Invoke(marker, null);
                SceneManager.SetActiveScene(normalScene);
                Assert.That(GuidePopupRuntime.IsSuppressed, Is.False);
                SceneManager.SetActiveScene(testScene);
                Assert.That(GuidePopupRuntime.IsSuppressed, Is.True);
                Assert.That(GuidePopupRuntime.Open(data), Is.Null);
                typeof(UIGuidePopupSuppression).GetMethod("OnDisable", flags).Invoke(marker, null);
                Assert.That(GuidePopupRuntime.IsSuppressed, Is.False);
            }
            finally
            {
                typeof(UIGuidePopupSuppression).GetMethod("OnDisable", flags).Invoke(marker, null);
                SceneManager.SetActiveScene(previous);
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(data);
                SceneManager.UnloadSceneAsync(testScene);
                SceneManager.UnloadSceneAsync(normalScene);
            }
            yield return new ExitPlayMode();
        }
    }
}
