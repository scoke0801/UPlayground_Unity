using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.EditorTools;
using UPlayGround.Manager;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.UI.Editor
{
    /// <summary>훈련용 연계 안내를 고정 프리팹으로 저작하고 기존 UI 데이터베이스에 연결한다.</summary>
    public static class UITrainingComboPrefabBuilder
    {
        private const string PrefabPath = "Assets/03.Prefabs/UI/HUD/UI_HUD_TrainingCombo.prefab";
        private const string TexturePath = "Assets/04.Images/UI/Training/TrainingComboPanel.png";
        private static TMP_FontAsset _font;
        private static InputGlyphDataSO _glyphs;

        /// <summary>기존 프리팹이 없는 경우에만 훈련 HUD를 생성한다.</summary>
        [UPlaygroundTool("UI.BuildTrainingCombo")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                throw new InvalidOperationException("이미 저작한 훈련 HUD가 있습니다. 프리팹에서 편집하세요.");
            var database = AssetDatabase.LoadAssetAtPath<UIPrefabDatabase>("Assets/10.Datas/Path/UIPrefabDatabase.asset");
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ExternalAssets/Font/Maplestory Light SDF.asset");
            _glyphs = AssetDatabase.LoadAssetAtPath<InputGlyphDataSO>("Assets/10.Datas/UI/Input/InputGlyphData.asset");
            if (database == null || _font == null || _glyphs == null)
                throw new InvalidOperationException("UI 데이터베이스·폰트·입력 글리프를 확인하세요.");
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("ImageGen 배경 이미지가 없습니다.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            // 생성 이미지의 바깥 매트는 쓰지 않고 조용한 내부 질감만 기존 프레임 안에 배치한다.
#pragma warning disable CS0618
            importer.spritesheet = new[] { new SpriteMetaData { name = "TrainingComboInset", rect = new Rect(96, 144, 1344, 720), pivot = new Vector2(0.5f, 0.5f), alignment = 0 } };
#pragma warning restore CS0618
            importer.SaveAndReimport();
            var inset = AssetDatabase.LoadAllAssetsAtPath(TexturePath).OfType<Sprite>().Single();
            var frame = AssetDatabase.LoadAllAssetsAtPath("Assets/04.Images/UI/Celadon/CeladonFrames.png")
                .OfType<Sprite>().Single(s => s.name == "Focus");
            var root = Rect("UI_HUD_TrainingCombo", null, new Vector2(450, 490), Vector2.zero);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0, 1);
            root.anchoredPosition = new Vector2(32, -200);
            root.localScale = Vector3.one * 1.1f;
            try
            {
                var hud = root.gameObject.AddComponent<UI_HUD_TrainingCombo>();
                var hudData = new SerializedObject(hud);
                hudData.FindProperty("_layer").intValue = (int)CanvasLayer.HUD;
                hudData.FindProperty("_canCloseWithEsc").boolValue = false;
                hudData.ApplyModifiedPropertiesWithoutUndo();
                var panel = Rect("Panel", root, new Vector2(450, 490), Vector2.zero);
                var group = panel.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;
                Image(panel, inset, false);
                Image(Rect("Frame", panel, new Vector2(450, 490), Vector2.zero), frame, true);
                Text("Title", panel, "연계 연습", new Vector2(22, -18), new Vector2(400, 30), 22);
                var feedback = Text("Feedback", panel, "약공격이나 스킬로 시작하세요", new Vector2(22, -52), new Vector2(406, 22), 16);
                var history = Row("Current", panel, new Vector2(22, -82));
                var rows = Rect("Routes", panel, new Vector2(406, 285), new Vector2(22, -142));
                var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childControlHeight = true; layout.childControlWidth = true;
                layout.childForceExpandHeight = false; layout.childForceExpandWidth = false;
                layout.spacing = 3;
                var template = Row("RouteTemplate", rows, Vector2.zero);
                template.gameObject.SetActive(false);
                var skill = Row("Skill", panel, new Vector2(22, -428));
                var hint = root.gameObject.AddComponent<UIComboRouteHint>();
                var data = new SerializedObject(hint);
                Set(data, "_rowContainer", rows); Set(data, "_rowTemplate", template);
                Set(data, "_panel", group); Set(data, "_historyRow", history);
                Set(data, "_skillRow", skill); Set(data, "_feedbackLabel", feedback);
                data.FindProperty("_trainingOnly").boolValue = true;
                data.FindProperty("_maxRows").intValue = 5;
                data.ApplyModifiedPropertiesWithoutUndo();
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
                Undo.RecordObject(database, "훈련 콤보 HUD 등록");
                database.AddPrefab(UI_HUD_TrainingCombo.UIKey, prefab, CanvasLayer.HUD, "훈련장에서만 표시하는 현재 연계와 후속 입력");
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
        }

        private static UIComboRouteHintRow Row(string name, Transform parent, Vector2 position)
        {
            var root = Rect(name, parent, new Vector2(406, 54), position);
            var element = root.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 54; element.preferredWidth = 406;
            var row = root.gameObject.AddComponent<UIComboRouteHintRow>();
            var label = Text("Label", root, "", new Vector2(0, 0), new Vector2(400, 22), 18);
            var status = Text("Status", root, "", new Vector2(190, -26), new Vector2(216, 24), 16);
            status.color = new Color(0.74f, 0.80f, 0.78f);
            var serialized = new SerializedObject(row);
            Set(serialized, "_skillLabel", label); Set(serialized, "_progressLabel", status);
            var icons = serialized.FindProperty("_sequenceIcons"); icons.arraySize = 4;
            var groups = serialized.FindProperty("_sequenceGroups"); groups.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                var key = Rect("Input" + i, root, new Vector2(34, 28), new Vector2(i * 46, -24));
                var group = key.gameObject.AddComponent<CanvasGroup>();
                if (i > 0) Text("Arrow", key, "→", new Vector2(-14, 0), new Vector2(14, 28), 13);
                var icon = key.gameObject.AddComponent<UIInputPromptIcon>();
                var image = Image(Rect("Glyph", key, new Vector2(28, 28), Vector2.zero), null, false);
                image.preserveAspect = true;
                var fallback = Text("Fallback", key, "", Vector2.zero, new Vector2(40, 28), 15);
                var data = new SerializedObject(icon);
                Set(data, "_iconImage", image); Set(data, "_fallbackLabel", fallback); Set(data, "_glyphData", _glyphs);
                data.ApplyModifiedPropertiesWithoutUndo();
                icons.GetArrayElementAtIndex(i).objectReferenceValue = icon;
                groups.GetArrayElementAtIndex(i).objectReferenceValue = group;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return row;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }

        private static Image Image(RectTransform rect, Sprite sprite, bool sliced)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.raycastTarget = false;
            image.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            return image;
        }

        private static TMP_Text Text(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
        {
            var text = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = _font; text.fontSize = fontSize; text.text = value;
            text.color = new Color(0.94f, 0.93f, 0.87f); text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static void Set(SerializedObject target, string field, UnityEngine.Object value) =>
            target.FindProperty(field).objectReferenceValue = value;
    }
}
