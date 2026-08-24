using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Dialogue;

namespace UPlayGround.UI.Tests
{
    /// <summary>대화 삽화 레이어의 프리팹 배선과 클릭 닫기 계약을 검증한다.</summary>
    public sealed class DialogueIllustrationPrefabTests
    {
        private const string PrefabPath =
            "Assets/03.Prefabs/UI/Scene/UI_Scene_Dialogue.prefab";
        private const string OpeningActionPath =
            "Assets/10.Datas/Dialogue/Story/Dialogue/Config/Action_ShowLakeOpeningProtagonist.asset";
        private const string OpeningFieldActionPath =
            "Assets/10.Datas/Dialogue/Story/Dialogue/Config/Action_ShowLakeOpeningField.asset";
        private const string OpeningDialoguePath =
            "Assets/10.Datas/Dialogue/Story/Dialogue/DLG_Lake_NewGameOpening.asset";
        private const string OpeningQuestPath =
            "Assets/10.Datas/Quest/Generated/SubStory/quest_sub_lake_missing_villagers.asset";

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
                Image foregroundIllustration = serializedDialogue
                    .FindProperty("illustrationForegroundImage")
                    .objectReferenceValue as Image;
                CanvasGroup illustrationGroup = serializedDialogue
                    .FindProperty("illustrationCanvasGroup")
                    .objectReferenceValue as CanvasGroup;
                GameObject dialoguePanel = serializedDialogue
                    .FindProperty("dialoguePanel")
                    .objectReferenceValue as GameObject;
                Component narration = serializedDialogue
                    .FindProperty("cinematicNarrationText")
                    .objectReferenceValue as Component;
                Component locationTitle = serializedDialogue
                    .FindProperty("cinematicLocationTitleText")
                    .objectReferenceValue as Component;

                Assert.That(illustration, Is.Not.Null);
                Assert.That(foregroundIllustration, Is.Not.Null);
                Assert.That(illustrationGroup, Is.Not.Null);
                Assert.That(dialoguePanel, Is.Not.Null);
                Assert.That(narration, Is.Not.Null);
                Assert.That(locationTitle, Is.Not.Null);
                Assert.That(illustrationGroup.gameObject.activeSelf, Is.False);
                Assert.That(illustration.transform.parent, Is.EqualTo(illustrationGroup.transform));
                Assert.That(
                    foregroundIllustration.transform.parent,
                    Is.EqualTo(illustrationGroup.transform));
                Assert.That(foregroundIllustration.gameObject.activeSelf, Is.False);
                Assert.That(foregroundIllustration.raycastTarget, Is.False);
                Assert.That(foregroundIllustration.preserveAspect, Is.True);
                Assert.That(
                    foregroundIllustration.transform.GetSiblingIndex(),
                    Is.GreaterThan(illustration.transform.GetSiblingIndex()));
                Assert.That(narration.transform.parent, Is.EqualTo(root.transform));
                Assert.That(locationTitle.transform.parent, Is.EqualTo(root.transform));
                Assert.That(narration.transform.IsChildOf(illustrationGroup.transform), Is.False);
                Assert.That(locationTitle.transform.IsChildOf(illustrationGroup.transform), Is.False);
                Assert.That(
                    narration.transform.GetSiblingIndex(),
                    Is.GreaterThan(illustrationGroup.transform.GetSiblingIndex()));
                Assert.That(
                    locationTitle.transform.GetSiblingIndex(),
                    Is.GreaterThan(illustrationGroup.transform.GetSiblingIndex()));
                Assert.That(illustration.raycastTarget, Is.True);
                Assert.That(illustration.preserveAspect, Is.False);
                AspectRatioFitter backgroundFitter = illustration.GetComponent<AspectRatioFitter>();
                Assert.That(backgroundFitter, Is.Not.Null);
                Assert.That(
                    backgroundFitter.aspectMode,
                    Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));
                Assert.That(illustrationGroup.interactable, Is.False);
                Assert.That(illustrationGroup.blocksRaycasts, Is.False);
                Assert.That(
                    illustrationGroup.transform.GetSiblingIndex(),
                    Is.GreaterThan(dialoguePanel.transform.GetSiblingIndex()));

                Image dim = illustrationGroup.transform
                    .Find("IllustrationDim")
                    ?.GetComponent<Image>();
                Assert.That(dim, Is.Not.Null);
                Assert.That(
                    foregroundIllustration.transform.GetSiblingIndex(),
                    Is.GreaterThan(dim.transform.GetSiblingIndex()));
                Assert.That(dim.raycastTarget, Is.True);
                Assert.That(dim.color.a, Is.InRange(0.35f, 0.45f));
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

        [Test]
        public void 새_게임_오프닝은_Field와_선택한_Player_삽화를_분리해_저작한다()
        {
            ScriptableObject fieldAction =
                AssetDatabase.LoadAssetAtPath<ScriptableObject>(OpeningFieldActionPath);
            ScriptableObject action = AssetDatabase.LoadAssetAtPath<ScriptableObject>(OpeningActionPath);
            Assert.That(fieldAction, Is.Not.Null);
            Assert.That(action, Is.Not.Null);

            var serializedFieldAction = new SerializedObject(fieldAction);
            var serializedAction = new SerializedObject(action);
            Sprite background = serializedAction
                .FindProperty("_backgroundIllustration")
                .objectReferenceValue as Sprite;
            SerializedProperty illustrations = serializedAction.FindProperty("_illustrations");

            Assert.That(
                serializedFieldAction.FindProperty("_showProtagonist").boolValue,
                Is.False);
            Assert.That(
                serializedFieldAction.FindProperty("_illustrations").arraySize,
                Is.Zero);
            Assert.That(
                serializedFieldAction.FindProperty("_motionDuration").floatValue,
                Is.EqualTo(9.6f).Within(0.001f));
            Assert.That(
                serializedAction.FindProperty("_showProtagonist").boolValue,
                Is.True);
            Assert.That(background, Is.Not.Null);
            Assert.That(background.name, Is.EqualTo("Story_Lake_Intro_Field"));
            Assert.That(illustrations.arraySize, Is.EqualTo(2));

            AssertCharacterIllustration(
                illustrations.GetArrayElementAtIndex(0),
                expectedCharacterType: 1,
                expectedSpriteName: "Story_Lake_Intro_Raon_Back3Q");
            AssertCharacterIllustration(
                illustrations.GetArrayElementAtIndex(1),
                expectedCharacterType: 13,
                expectedSpriteName: "Story_Lake_Intro_Arin_Back3Q");
            Assert.That(
                serializedAction.FindProperty("_fallbackIllustration").objectReferenceValue,
                Is.Null);
        }

        [Test]
        public void 새_게임_오프닝의_노출_노드는_모두_시네마틱_텍스트를_가진다()
        {
            DialogueGraphSO graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(OpeningDialoguePath);
            Assert.That(graph, Is.Not.Null);
            Assert.That(graph.nodes, Is.Not.Empty);

            int presentationNodeCount = 0;
            float sequenceDuration = 0f;
            DialogueNodeSO firstNode = null;
            DialogueNodeSO protagonistNode = null;
            DialogueNodeSO titleNode = null;
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DialogueNodeSO node = graph.nodes[i];
                if (node == null || node.nodeType == NodeType.End)
                    continue;

                presentationNodeCount++;
                sequenceDuration += node.autoAdvanceDuration;
                Assert.That(node.dialogueText, Is.Not.Empty, node.nodeId);
                Assert.That(node.dialogueText, Does.Not.StartWith("목표:"), node.nodeId);
                Assert.That(
                    node.textPresentation,
                    Is.Not.EqualTo(DialogueTextPresentation.Standard),
                    node.nodeId);

                if (node.nodeId == "lake_opening_01")
                    firstNode = node;
                else if (node.nodeId == "lake_opening_02")
                    protagonistNode = node;
                else if (node.nodeId == "lake_opening_title")
                    titleNode = node;
            }

            Assert.That(presentationNodeCount, Is.EqualTo(3));
            Assert.That(sequenceDuration, Is.EqualTo(9.6f).Within(0.001f));
            Assert.That(firstNode, Is.Not.Null);
            Assert.That(protagonistNode, Is.Not.Null);
            Assert.That(titleNode, Is.Not.Null);
            Assert.That(firstNode.eventActions, Has.Count.EqualTo(1));
            Assert.That(firstNode.eventActions[0].name, Is.EqualTo("Action_ShowLakeOpeningField"));
            Assert.That(protagonistNode.eventActions, Has.Count.EqualTo(1));
            Assert.That(
                protagonistNode.eventActions[0].name,
                Is.EqualTo("Action_ShowLakeOpeningProtagonist"));
            Assert.That(titleNode.nextNodeId, Is.EqualTo("lake_opening_end"));
        }

        [Test]
        public void 오프닝_이후_퀘스트는_실제_첫_목표를_안내한다()
        {
            ScriptableObject quest = AssetDatabase.LoadAssetAtPath<ScriptableObject>(OpeningQuestPath);
            Assert.That(quest, Is.Not.Null);

            var serializedQuest = new SerializedObject(quest);
            string summary = serializedQuest.FindProperty("shortSummary").stringValue;
            SerializedProperty objectives = serializedQuest.FindProperty("objectives");
            string firstObjective = objectives.GetArrayElementAtIndex(0)
                .FindPropertyRelative("description")
                .stringValue;

            Assert.That(summary, Does.Contain("안내인"));
            Assert.That(firstObjective, Does.Contain("안내인"));
        }

        [Test]
        public void 대화_UI를_초기화한_뒤에도_삽화를_다시_표시할_수_있다()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));

            try
            {
                UI_Scene_Dialogue dialogue = root.GetComponent<UI_Scene_Dialogue>();
                Assert.That(dialogue, Is.Not.Null);

                const System.Reflection.BindingFlags Flags =
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic;
                System.Reflection.MethodInfo cache = typeof(UI_Scene_Dialogue)
                    .GetMethod("CachePresentationReferences", Flags);
                System.Reflection.MethodInfo reset = typeof(UI_Scene_Dialogue)
                    .GetMethod("ResetPresentation", Flags);
                System.Reflection.MethodInfo apply = typeof(UI_Scene_Dialogue)
                    .GetMethod(
                        "ApplyIllustration",
                        Flags,
                        null,
                        new[]
                        {
                            typeof(Sprite),
                            typeof(Sprite),
                            typeof(Color),
                            typeof(DialogueIllustrationPresentation)
                        },
                        null);
                Image foregroundIllustration = new SerializedObject(dialogue)
                    .FindProperty("illustrationForegroundImage")
                    .objectReferenceValue as Image;

                Assert.That(cache, Is.Not.Null);
                Assert.That(reset, Is.Not.Null);
                Assert.That(apply, Is.Not.Null);
                Assert.That(foregroundIllustration, Is.Not.Null);

                cache.Invoke(dialogue, null);
                var presentation = new DialogueIllustrationPresentation(
                    Vector2.zero,
                    Vector2.zero,
                    1f,
                    1.05f,
                    1f,
                    revealImmediately: true,
                    DialogueIllustrationPlacement.BehindDialogue,
                    DialogueIllustrationPresentationMode.CinematicNarration,
                    persistAcrossFollowingLines: true);

                apply.Invoke(dialogue, new object[] { sprite, sprite, Color.white, presentation });
                Assert.That(dialogue.IsIllustrationVisible, Is.True);
                Assert.That(foregroundIllustration.gameObject.activeSelf, Is.True);
                Assert.That(foregroundIllustration.sprite, Is.SameAs(sprite));

                reset.Invoke(dialogue, null);
                Assert.That(dialogue.IsIllustrationVisible, Is.False);
                Assert.That(foregroundIllustration.gameObject.activeSelf, Is.False);
                Assert.That(foregroundIllustration.sprite, Is.Null);

                apply.Invoke(dialogue, new object[] { sprite, sprite, Color.white, presentation });
                Assert.That(dialogue.IsIllustrationVisible, Is.True);
                Assert.That(foregroundIllustration.gameObject.activeSelf, Is.True);

                reset.Invoke(dialogue, null);
            }
            finally
            {
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void 일반_대화_삽화는_화면_안에_맞추고_새_게임_오프닝만_전체화면을_채운다()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            var texture = new Texture2D(4, 2, TextureFormat.RGBA32, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 4, 2), new Vector2(0.5f, 0.5f));

            try
            {
                UI_Scene_Dialogue dialogue = root.GetComponent<UI_Scene_Dialogue>();
                var serializedDialogue = new SerializedObject(dialogue);
                Image illustration = serializedDialogue
                    .FindProperty("illustrationImage")
                    .objectReferenceValue as Image;
                AspectRatioFitter aspectRatioFitter = illustration != null
                    ? illustration.GetComponent<AspectRatioFitter>()
                    : null;

                const System.Reflection.BindingFlags Flags =
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic;
                System.Reflection.MethodInfo cache = typeof(UI_Scene_Dialogue)
                    .GetMethod("CachePresentationReferences", Flags);
                System.Reflection.MethodInfo reset = typeof(UI_Scene_Dialogue)
                    .GetMethod("ResetPresentation", Flags);
                System.Reflection.MethodInfo apply = typeof(UI_Scene_Dialogue)
                    .GetMethod(
                        "ApplyIllustration",
                        Flags,
                        null,
                        new[]
                        {
                            typeof(Sprite),
                            typeof(Sprite),
                            typeof(Color),
                            typeof(DialogueIllustrationPresentation)
                        },
                        null);

                Assert.That(dialogue, Is.Not.Null);
                Assert.That(illustration, Is.Not.Null);
                Assert.That(aspectRatioFitter, Is.Not.Null);
                Assert.That(cache, Is.Not.Null);
                Assert.That(reset, Is.Not.Null);
                Assert.That(apply, Is.Not.Null);

                cache.Invoke(dialogue, null);
                apply.Invoke(
                    dialogue,
                    new object[]
                    {
                        sprite,
                        null,
                        Color.white,
                        DialogueIllustrationPresentation.None
                    });

                Assert.That(illustration.preserveAspect, Is.True);
                Assert.That(
                    aspectRatioFitter.aspectMode,
                    Is.EqualTo(AspectRatioFitter.AspectMode.None));

                var openingPresentation = new DialogueIllustrationPresentation(
                    Vector2.zero,
                    Vector2.zero,
                    1f,
                    1.05f,
                    1f,
                    revealImmediately: true,
                    DialogueIllustrationPlacement.BehindDialogue,
                    DialogueIllustrationPresentationMode.CinematicNarration,
                    persistAcrossFollowingLines: true);
                apply.Invoke(
                    dialogue,
                    new object[] { sprite, null, Color.white, openingPresentation });

                Assert.That(illustration.preserveAspect, Is.False);
                Assert.That(
                    aspectRatioFitter.aspectMode,
                    Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));

                reset.Invoke(dialogue, null);
            }
            finally
            {
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void 시네마틱_노드는_삽화_상태와_무관하게_텍스트를_즉시_표시한다()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            DialogueNodeSO node = ScriptableObject.CreateInstance<DialogueNodeSO>();

            try
            {
                UI_Scene_Dialogue dialogue = root.GetComponent<UI_Scene_Dialogue>();
                var serializedDialogue = new SerializedObject(dialogue);
                Component narration = serializedDialogue
                    .FindProperty("cinematicNarrationText")
                    .objectReferenceValue as Component;

                node.dialogueText = "호숫가의 오래된 신전";
                node.textPresentation = DialogueTextPresentation.CinematicNarration;

                const System.Reflection.BindingFlags InstanceFlags =
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic;
                const System.Reflection.BindingFlags StaticFlags =
                    System.Reflection.BindingFlags.Static
                    | System.Reflection.BindingFlags.NonPublic;
                System.Reflection.MethodInfo usesCinematicText = typeof(UI_Scene_Dialogue)
                    .GetMethod("UsesCinematicText", StaticFlags);
                System.Reflection.MethodInfo setCinematicNarrationActive =
                    typeof(UI_Scene_Dialogue).GetMethod(
                        "SetCinematicNarrationActive",
                        InstanceFlags);

                Assert.That(dialogue, Is.Not.Null);
                Assert.That(narration, Is.Not.Null);
                Assert.That(usesCinematicText, Is.Not.Null);
                Assert.That(setCinematicNarrationActive, Is.Not.Null);
                Assert.That(usesCinematicText.Invoke(null, new object[] { node }), Is.True);

                setCinematicNarrationActive.Invoke(dialogue, new object[] { true, node });

                Assert.That(narration.gameObject.activeSelf, Is.True);
                Assert.That(
                    narration.GetType().GetProperty("text")?.GetValue(narration),
                    Is.EqualTo(node.dialogueText));
                Assert.That(
                    narration.GetType().GetProperty("alpha")?.GetValue(narration),
                    Is.LessThan(1f));
                Assert.That(
                    narration.transform.GetSiblingIndex(),
                    Is.EqualTo(root.transform.childCount - 1));

                setCinematicNarrationActive.Invoke(dialogue, new object[] { false, null });
            }
            finally
            {
                Object.DestroyImmediate(node);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void 지속형_시네마틱_삽화는_대사_진행_입력으로_닫히지_않는다()
        {
            const System.Reflection.BindingFlags Flags =
                System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.NonPublic;
            System.Reflection.MethodInfo canDismiss = typeof(UI_Scene_Dialogue)
                .GetMethod("CanDismissIllustrationOnAdvance", Flags);
            Assert.That(canDismiss, Is.Not.Null);

            var persistentPresentation = new DialogueIllustrationPresentation(
                Vector2.zero,
                Vector2.zero,
                1f,
                1f,
                1f,
                persistAcrossFollowingLines: true);

            Assert.That(
                canDismiss.Invoke(null, new object[] { persistentPresentation }),
                Is.False);
            Assert.That(
                canDismiss.Invoke(
                    null,
                    new object[] { DialogueIllustrationPresentation.None }),
                Is.True);
        }

        private static void AssertCharacterIllustration(
            SerializedProperty entry,
            int expectedCharacterType,
            string expectedSpriteName)
        {
            Assert.That(
                entry.FindPropertyRelative("characterType").intValue,
                Is.EqualTo(expectedCharacterType));
            Sprite illustration = entry
                .FindPropertyRelative("illustration")
                .objectReferenceValue as Sprite;
            Vector2 startOffset = entry.FindPropertyRelative("startOffset").vector2Value;
            Vector2 endOffset = entry.FindPropertyRelative("endOffset").vector2Value;
            Assert.That(illustration, Is.Not.Null);
            Assert.That(illustration.name, Is.EqualTo(expectedSpriteName));
            Assert.That(endOffset.x, Is.GreaterThanOrEqualTo(450f));
            Assert.That(startOffset.x, Is.GreaterThan(endOffset.x));
            Assert.That(entry.FindPropertyRelative("startScale").floatValue, Is.GreaterThan(0.9f));
            Assert.That(entry.FindPropertyRelative("endScale").floatValue, Is.GreaterThan(0.9f));
            Assert.That(entry.FindPropertyRelative("enterDuration").floatValue, Is.GreaterThan(0f));
        }
    }
}
