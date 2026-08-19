using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.Tests
{
    /// <summary>대화 삽화 레이어의 프리팹 배선과 클릭 닫기 계약을 검증한다.</summary>
    public sealed class DialogueIllustrationPrefabTests
    {
        private const string PrefabPath =
            "Assets/03.Prefabs/UI/Scene/UI_Scene_Dialogue.prefab";

        [Test]
        public void 대화_삽화는_패널_위에서_클릭을_받고_초기에는_숨겨진다()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                UI_Scene_Dialogue dialogue = root.GetComponent<UI_Scene_Dialogue>();
                Assert.That(dialogue, Is.Not.Null);
                Assert.That(dialogue.IsIllustrationVisible, Is.False);
                Assert.That(root.GetComponent<GraphicRaycaster>(), Is.Not.Null);

                var serializedDialogue = new SerializedObject(dialogue);
                Image illustration = serializedDialogue
                    .FindProperty("illustrationImage")
                    .objectReferenceValue as Image;
                CanvasGroup illustrationGroup = serializedDialogue
                    .FindProperty("illustrationCanvasGroup")
                    .objectReferenceValue as CanvasGroup;
                GameObject dialoguePanel = serializedDialogue
                    .FindProperty("dialoguePanel")
                    .objectReferenceValue as GameObject;

                Assert.That(illustration, Is.Not.Null);
                Assert.That(illustrationGroup, Is.Not.Null);
                Assert.That(dialoguePanel, Is.Not.Null);
                Assert.That(illustrationGroup.gameObject.activeSelf, Is.False);
                Assert.That(illustration.transform.parent, Is.EqualTo(illustrationGroup.transform));
                Assert.That(illustration.raycastTarget, Is.True);
                Assert.That(illustration.preserveAspect, Is.True);
                Assert.That(illustrationGroup.interactable, Is.False);
                Assert.That(illustrationGroup.blocksRaycasts, Is.False);
                Assert.That(
                    illustrationGroup.transform.GetSiblingIndex(),
                    Is.GreaterThan(dialoguePanel.transform.GetSiblingIndex()));

                Image dim = illustrationGroup.transform
                    .Find("IllustrationDim")
                    ?.GetComponent<Image>();
                Assert.That(dim, Is.Not.Null);
                Assert.That(dim.raycastTarget, Is.True);
                Assert.That(dim.color.a, Is.GreaterThanOrEqualTo(0.5f));
                RectTransform dimRect = dim.rectTransform;
                Assert.That(dimRect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(dimRect.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(dimRect.sizeDelta, Is.EqualTo(Vector2.zero));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
