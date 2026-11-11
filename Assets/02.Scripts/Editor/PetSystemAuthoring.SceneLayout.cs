using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Pet;
using UPlayGround.UI;

namespace UPlayGround.EditorTools
{
    public static partial class PetSystemAuthoring
    {
        private static void ApplyFullScreenLayout(GameObject root, UIVisualThemeSO theme)
        {
            var parent = root.transform;
            s_font = root.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.font != null).font;
            Pin(parent.Find("Title"), new Vector2(0, 1), new Vector2(96, -76), new Vector2(480, 76), new Vector2(0, .5f));
            StyleText(parent.Find("Title"), 56, TextAlignmentOptions.MidlineLeft);
            Pin(parent.Find("PetName"), new Vector2(0, 1), new Vector2(100, -146), new Vector2(480, 48), new Vector2(0, .5f));
            StyleText(parent.Find("PetName"), 30, TextAlignmentOptions.MidlineLeft);
            Pin(parent.Find("Bond"), new Vector2(1, 1), new Vector2(-96, -90), new Vector2(640, 50), new Vector2(1, .5f));
            StyleText(parent.Find("Bond"), 30, TextAlignmentOptions.MidlineRight);
            Pin(parent.Find("PetTabs"), new Vector2(.5f, 1), new Vector2(0, -124), new Vector2(920, 76));
            for (int i = 0; i < 2; i++)
            {
                var tab = parent.Find("PetTabs/Tab" + i);
                Place(tab, new Vector2(i == 0 ? -232 : 232, 0), new Vector2(432, 76));
                StyleText(tab.GetComponentInChildren<TextMeshProUGUI>().transform, 32, TextAlignmentOptions.Center);
            }
            Place(parent.Find("PetTabs/TabPrompt0"), new Vector2(-510, 0), new Vector2(44, 44));
            Place(parent.Find("PetTabs/TabPrompt1"), new Vector2(510, 0), new Vector2(44, 44));
            AddRule(parent, "HeaderRule", 1f, -216f, theme);
            AddRule(parent, "FooterRule", 0f, 166f, theme);
            Pin(parent.Find("Accompany"), Vector2.zero, new Vector2(96, 76), new Vector2(384, 76), new Vector2(0, .5f));
            Pin(parent.Find("Close"), new Vector2(1, 0), new Vector2(-96, 76), new Vector2(320, 76), new Vector2(1, .5f));
            Pin(parent.Find("Status"), Vector2.zero, new Vector2(96, 204), new Vector2(1700, 50), new Vector2(0, .5f));
            StyleText(parent.Find("Status"), 28, TextAlignmentOptions.MidlineLeft);
            Pin(parent.Find("RetrySave"), new Vector2(1, 0), new Vector2(-96, 204), new Vector2(340, 64), new Vector2(1, .5f));
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == "SkillTemplate") continue;
                var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                label.fontSize = 30; label.enableAutoSizing = false;
            }

            var model = parent.Find("V2ModelPage");
            var skill = parent.Find("V2SkillPage");
            foreach (var page in new[] { model, skill })
                Stretch(page, Vector2.zero, Vector2.one, new Vector2(96, 260), new Vector2(-96, -256));
            var preview = model.Find("Preview");
            Stretch(preview, new Vector2(.025f, .10f), new Vector2(.56f, .97f), Vector2.zero, Vector2.zero);
            var previewData = new SerializedObject(preview.GetComponent<UICharacterPreviewRenderer>());
            var display = preview.Find("Display");
            if (display == null) display = RectObject("Display", preview, Vector2.zero, Vector2.zero).transform;
            var raw = display.GetComponent<RawImage>() ?? display.gameObject.AddComponent<RawImage>();
            var aspect = display.GetComponent<AspectRatioFitter>() ?? display.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio = 1f;
            preview.GetComponent<RawImage>().enabled = false;
            previewData.FindProperty("_display").objectReferenceValue = raw;
            previewData.FindProperty("_textureSize").intValue = 1536;
            previewData.FindProperty("_cameraDistanceScale").floatValue = .86f;
            previewData.ApplyModifiedPropertiesWithoutUndo();
            var orbit = EnsureImage(model, "PreviewOrnament", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/BorderFrame_Circle.png"));
            Stretch(orbit.transform, new Vector2(.035f, .10f), new Vector2(.55f, .97f), Vector2.zero, Vector2.zero);
            orbit.preserveAspect = true; orbit.color = new Color(.68f, .79f, .74f, .10f);
            orbit.type = Image.Type.Simple;
            orbit.transform.SetAsFirstSibling();
            Pin(model.Find("Model"), new Vector2(.29f, 0), new Vector2(0, 38), new Vector2(420, 72));
            var looks = EnsureImage(model, "AppearancePanel", theme.CardFrame);
            Stretch(looks.transform, new Vector2(.59f, 0), Vector2.one, Vector2.zero, Vector2.zero);
            looks.transform.SetAsFirstSibling();
            var looksTitle = EnsureText(model, "AppearanceTitle", "외형 선택", 38);
            Pin(looksTitle.transform, new Vector2(.62f, 1), new Vector2(0, -64), new Vector2(700, 56), new Vector2(0, .5f));
            var looksHint = EnsureText(model, "AppearanceHint", "마음에 드는 모습을 골라 보세요.", 26);
            looksHint.color = theme.TextSub;
            Pin(looksHint.transform, new Vector2(.62f, 1), new Vector2(0, -126), new Vector2(760, 44), new Vector2(0, .5f));
            Stretch(model.Find("Appearances"), new Vector2(.62f, .18f), new Vector2(.97f, .79f), Vector2.zero, Vector2.zero);
            var grid = model.Find("Appearances").GetComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(20, 18); grid.childAlignment = TextAnchor.MiddleCenter;
            grid.cellSize = new Vector2(390, 110);
            var template = model.Find("Appearances/Appearance");
            var portrait = EnsureImage(template, "Portrait", null);
            portrait.type = Image.Type.Simple;
            portrait.preserveAspect = true;
            Pin(portrait.transform, new Vector2(0, .5f), new Vector2(66, 4), new Vector2(108, 108));
            var labelTransform = template.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.name != "Equipped").transform;
            Stretch(labelTransform, new Vector2(.36f, .25f), new Vector2(.98f, .8f), Vector2.zero, Vector2.zero);
            StyleText(labelTransform, 28, TextAlignmentOptions.Center);
            var equipped = EnsureText(template, "Equipped", "동행 중", 20);
            equipped.color = theme.Positive;
            Pin(equipped.transform, new Vector2(.68f, 0), new Vector2(0, 20), new Vector2(180, 26));
            equipped.alignment = TextAlignmentOptions.Center;
            Pin(model.Find("Apply"), new Vector2(.795f, 0), new Vector2(0, 66), new Vector2(760, 76));

            var graph = skill.Find("SkillList");
            Stretch(graph, Vector2.zero, new Vector2(.66f, 1), Vector2.zero, new Vector2(-32, 0));
            var art = graph.Find("GraphArt");
            Stretch(art, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            foreach (var curve in art.GetComponentsInChildren<UISkillTreeCurve>(true))
                Stretch(curve.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Vector2[] anchors = { new(.3f, .73f), new(.7f, .73f), new(.16f, .22f), new(.5f, .22f), new(.84f, .22f) };
            for (int i = 0; i < anchors.Length; i++)
                Pin(graph.Find("Slot" + i), anchors[i], Vector2.zero, new Vector2(250, 248));
            Pin(art.Find("Explore"), new Vector2(.5f, .96f), Vector2.zero, new Vector2(520, 56));
            Pin(art.Find("Support"), new Vector2(.5f, .45f), Vector2.zero, new Vector2(520, 56));
            StyleText(art.Find("Explore"), 36, TextAlignmentOptions.Center);
            StyleText(art.Find("Support"), 36, TextAlignmentOptions.Center);
            art.Find("Support").GetComponent<TextMeshProUGUI>().text = "전투 지원 / 하나 선택";
            Pin(art.Find("Hint"), new Vector2(.5f, 0), new Vector2(0, 8), new Vector2(1100, 44));
            StyleText(art.Find("Hint"), 26, TextAlignmentOptions.Center);
            var node = graph.Find("SkillTemplate");
            Place(node, Vector2.zero, new Vector2(250, 248));
            Place(node.Find("NodeFrame"), new Vector2(0, 30), new Vector2(160, 160));
            Place(node.Find("SkillIcon"), new Vector2(0, 30), new Vector2(112, 112));
            var nodeName = node.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.name != "Rank");
            Place(nodeName.transform, new Vector2(0, -78), new Vector2(290, 48));
            StyleText(nodeName.transform, 32, TextAlignmentOptions.Center);
            Place(node.Find("Rank"), new Vector2(0, -122), new Vector2(310, 36));
            StyleText(node.Find("Rank"), 25, TextAlignmentOptions.Center);
            var detail = skill.Find("DetailPanel");
            Stretch(detail, new Vector2(.69f, 0), Vector2.one, Vector2.zero, Vector2.zero);
            Stretch(detail.Find("SkillDetail"), Vector2.zero, Vector2.one, new Vector2(48, 240), new Vector2(-48, -54));
            StyleText(detail.Find("SkillDetail"), 30, TextAlignmentOptions.TopLeft);
            Stretch(detail.Find("SelectSkill"), Vector2.zero, new Vector2(1, 0), new Vector2(48, 142), new Vector2(-48, 220));
            Stretch(detail.Find("Upgrade"), Vector2.zero, new Vector2(1, 0), new Vector2(48, 42), new Vector2(-48, 120));
            Canvas.ForceUpdateCanvases();
        }

        private static void Pin(Transform target, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            var rect = (RectTransform)target;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot ?? new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        private static void Stretch(Transform target, Vector2 min, Vector2 max, Vector2 insetMin, Vector2 insetMax)
        {
            var rect = (RectTransform)target;
            rect.anchorMin = min; rect.anchorMax = max; rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = insetMin; rect.offsetMax = insetMax;
        }

        private static Image EnsureImage(Transform parent, string name, Sprite sprite)
        {
            var child = parent.Find(name) ?? RectObject(name, parent, Vector2.zero, Vector2.zero).transform;
            var image = child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.type = Image.Type.Sliced;
            image.color = Color.white; image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string name, string value, float size)
        {
            var text = parent.Find(name)?.GetComponent<TextMeshProUGUI>();
            if (text == null) text = Text(parent, name, value, new Vector2(600, 50), Vector2.zero, size);
            text.text = value; StyleText(text.transform, size, TextAlignmentOptions.MidlineLeft);
            return text;
        }

        private static void StyleText(Transform target, float size, TextAlignmentOptions alignment)
        {
            var text = target.GetComponent<TextMeshProUGUI>();
            text.fontSize = size; text.enableAutoSizing = false; text.alignment = alignment;
            text.raycastTarget = false;
        }

        private static void AddRule(Transform parent, string name, float anchorY, float offset, UIVisualThemeSO theme)
        {
            var line = EnsureImage(parent, name, null);
            Stretch(line.transform, new Vector2(0, anchorY), new Vector2(1, anchorY), new Vector2(96, offset), new Vector2(-96, offset + 2));
            line.color = new Color(theme.Focus.r, theme.Focus.g, theme.Focus.b, .28f);
        }

        /// <summary>기존 펫 프리뷰와 외형 재질에서 UI용 썸네일을 고정 렌더한다.</summary>
        [UPlaygroundTool("PetSystem.BakeAppearancePortraits")]
        public static void BakeAppearancePortraits()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일이 끝난 Edit Mode에서 실행해야 합니다.");
            const string folder = "Assets/04.Images/UI/Pet/Appearances";
            Directory.CreateDirectory(folder);
            var definition = AssetDatabase.LoadAssetAtPath<PetDefinitionSO>(DataRoot + "Monkey.asset");
            var holder = new GameObject("PetPortraitBake", typeof(RectTransform), typeof(RawImage));
            var preview = holder.AddComponent<UICharacterPreviewRenderer>();
            var serialized = new SerializedObject(preview);
            serialized.FindProperty("_display").objectReferenceValue = holder.GetComponent<RawImage>();
            serialized.FindProperty("_textureSize").intValue = 256;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var previous = RenderTexture.active;
            try
            {
                for (int i = 0; i < definition.appearances.Count; i++)
                {
                    var appearance = definition.appearances[i];
                    if (!preview.ShowPetPreview(definition, appearance.appearanceId))
                        throw new InvalidOperationException("펫 썸네일 프리뷰 실패: " + appearance.appearanceId);
                    var camera = (Camera)typeof(UICharacterPreviewRenderer).GetField("_previewCamera", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(preview);
                    camera.Render();
                    var target = preview.GetRenderTexture();
                    RenderTexture.active = target;
                    var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                    try
                    {
                        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
                        string path = folder + "/Monkey_" + i.ToString("00") + ".png";
                        File.WriteAllBytes(path, texture.EncodeToPNG());
                        AssetDatabase.ImportAsset(path);
                        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                        importer.alphaIsTransparency = true;
                        importer.mipmapEnabled = false; importer.SaveAndReimport();
                        appearance.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        if (appearance.portrait == null) throw new InvalidOperationException("외형 썸네일 임포트 실패: " + path);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(texture); }
                }
                // 임포트 중 SO가 다시 로드될 수 있으므로 참조 연결은 모든 임포트 뒤에 적용한다.
                var definitionData = new SerializedObject(definition);
                var appearances = definitionData.FindProperty("appearances");
                for (int i = 0; i < appearances.arraySize; i++)
                    appearances.GetArrayElementAtIndex(i).FindPropertyRelative("portrait").objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<Sprite>(folder + "/Monkey_" + i.ToString("00") + ".png");
                definitionData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition); AssetDatabase.SaveAssetIfDirty(definition);
            }
            finally
            {
                RenderTexture.active = previous;
                preview.HidePreview();
                var camera = (Camera)typeof(UICharacterPreviewRenderer).GetField("_previewCamera", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(preview);
                if (camera != null) camera.targetTexture = null;
                var target = preview.GetRenderTexture();
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                var stage = (GameObject)typeof(UICharacterPreviewRenderer).GetField("_previewStage", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(preview);
                if (stage != null) UnityEngine.Object.DestroyImmediate(stage);
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }
    }
}
