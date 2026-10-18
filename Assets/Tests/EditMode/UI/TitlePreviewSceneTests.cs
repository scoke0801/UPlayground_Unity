using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UPlayGround.UI.Tests
{
    /// <summary>타이틀 배경의 장비가 캐릭터 프리뷰에 겹쳐 보이지 않는지 검사한다.</summary>
    public sealed class TitlePreviewSceneTests
    {
        [Test]
        public void 타이틀_배경의_무기는_프리뷰_뒤에_노출되지_않는다()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/01.Scenes/GameLogic/Title.unity");
            try
            {
                var weaponRenderers = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                    .Where(renderer => renderer.CompareTag("Weapon")).ToArray();
                Assert.That(weaponRenderers, Is.Not.Empty, "보존한 타이틀 장비 배치를 검사해야 한다.");
                foreach (var renderer in weaponRenderers)
                    Assert.That(renderer.enabled && renderer.gameObject.activeInHierarchy, Is.False,
                        renderer.name + ": 배경 카메라에 무기가 노출된다.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
