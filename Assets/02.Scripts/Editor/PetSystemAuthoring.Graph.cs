using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.UI;
using UPlayGround.Manager;

namespace UPlayGround.EditorTools
{
    public static partial class PetSystemAuthoring
    {
        /// <summary>기존 스킬 트리의 노드와 흐름 머티리얼로 펫 화면을 고정 저작한다.</summary>
        [UPlaygroundTool("PetSystem.ApplyGraphLayout")]
        public static void ApplyGraphLayout()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일이 끝난 Edit Mode에서 실행해야 합니다.");
            const string path = "Assets/03.Prefabs/UI/Scene/Pet/UI_Scene_PetMenu.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var menu = root.GetComponent<UI_Scene_PetMenu>();
                var data = new SerializedObject(menu);
                s_font = root.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.font != null).font;
                var plain = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(1520, 900);
                var background = root.GetComponent<Image>();
                background.sprite = plain; background.type = Image.Type.Sliced;
                background.color = new Color(.055f, .075f, .105f, .98f);
                root.transform.Find("TitleDecoration")?.gameObject.SetActive(false);
                Place(root.transform.Find("Title"), new Vector2(0, 390), new Vector2(900, 56));
                Place(root.transform.Find("PetName"), new Vector2(-510, 333), new Vector2(380, 44));
                Place(root.transform.Find("Bond"), new Vector2(390, 333), new Vector2(620, 44));
                Place(root.transform.Find("PetTabs"), new Vector2(0, 270), new Vector2(900, 52));
                Place(root.transform.Find("Status"), new Vector2(-160, -325), new Vector2(1080, 56));
                Place(root.transform.Find("RetrySave"), new Vector2(550, -325), new Vector2(280, 52));
                Place(root.transform.Find("Accompany"), new Vector2(-170, -395), new Vector2(280, 58));
                Place(root.transform.Find("Close"), new Vector2(170, -395), new Vector2(280, 58));
                foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    label.color = new Color(.94f, .96f, .97f);
                    label.fontSize = 26; label.enableAutoSizing = false;
                    label.raycastTarget = false;
                }
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    var image = button.GetComponent<Image>();
                    image.overrideSprite = null; image.sprite = plain; image.type = Image.Type.Sliced;
                    image.color = new Color(.14f, .2f, .25f);
                    var colors = button.colors;
                    colors.normalColor = Color.white; colors.selectedColor = new Color(.65f, 1f, .88f);
                    colors.highlightedColor = colors.selectedColor; colors.disabledColor = new Color(.5f, .5f, .5f, .75f);
                    button.colors = colors;
                }
                var model = root.transform.Find("V2ModelPage");
                Place(model, new Vector2(0, -20), new Vector2(1420, 500));
                Place(model.Find("Preview"), new Vector2(-350, 25), new Vector2(500, 430));
                Place(model.Find("Model"), new Vector2(-350, -220), new Vector2(360, 54));
                Place(model.Find("Apply"), new Vector2(380, -220), new Vector2(430, 54));
                Place(model.Find("Appearances"), new Vector2(380, 25), new Vector2(410, 410));
                var grid = model.Find("Appearances").GetComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(190, 64); grid.spacing = new Vector2(20, 15);
                var skill = root.transform.Find("V2SkillPage");
                Place(skill, new Vector2(0, -20), new Vector2(1420, 500));
                var graph = skill.Find("SkillList");
                Place(graph, new Vector2(-270, 0), new Vector2(880, 500));
                var layout = graph.GetComponent<VerticalLayoutGroup>();
                if (layout != null) UnityEngine.Object.DestroyImmediate(layout);
                var old = graph.Find("GraphArt");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var art = RectObject("GraphArt", graph, new Vector2(880, 500), Vector2.zero).transform;
                art.SetAsFirstSibling();
                var detail = skill.Find("DetailPanel");
                if (detail == null) detail = RectObject("DetailPanel", skill, new Vector2(460, 500), new Vector2(480, 0)).transform;
                var card = detail.GetComponent<Image>() ?? detail.gameObject.AddComponent<Image>();
                card.sprite = plain; card.type = Image.Type.Sliced; card.color = new Color(.11f, .14f, .19f);
                card.raycastTarget = false;
                var text = skill.Find("SkillDetail") ?? detail.Find("SkillDetail");
                text.SetParent(detail, false); Place(text, new Vector2(0, 65), new Vector2(400, 330));
                text.GetComponent<TextMeshProUGUI>().fontSize = 24;
                var select = skill.Find("SelectSkill") ?? detail.Find("SelectSkill");
                select.SetParent(detail, false); Place(select, new Vector2(0, -140), new Vector2(400, 56));
                var upgrade = skill.Find("Upgrade") ?? detail.Find("Upgrade");
                upgrade.SetParent(detail, false); Place(upgrade, new Vector2(0, -208), new Vector2(400, 56));
                var template = graph.Find("SkillTemplate");
                Place(template, Vector2.zero, new Vector2(154, 176));
                var templateBackground = template.GetComponent<Image>();
                templateBackground.color = new Color(1, 1, 1, 0); templateBackground.sprite = null;
                template.GetComponent<Button>().transition = Selectable.Transition.None;
                var frame = template.Find("NodeFrame");
                if (frame == null) frame = RectObject("NodeFrame", template, new Vector2(104, 104), new Vector2(0, 24)).transform;
                var frameImage = frame.GetComponent<Image>() ?? frame.gameObject.AddComponent<Image>();
                frameImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ExternalAssets/UI/Artsystack - Fantasy RPG GUI/ResourcesData/Sprites/components/circle_slot_01.png");
                frameImage.preserveAspect = true; frameImage.raycastTarget = false; frame.SetAsFirstSibling();
                Place(template.Find("SkillIcon"), new Vector2(0, 24), new Vector2(72, 72));
                var nameLabel = template.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.name != "Rank");
                Place(nameLabel.transform, new Vector2(0, -43), new Vector2(190, 36));
                nameLabel.fontSize = 24;
                var rank = template.Find("Rank")?.GetComponent<TextMeshProUGUI>();
                if (rank == null) rank = Text(template, "Rank", "Lv.1 / 5", new Vector2(200, 30), new Vector2(0, -73), 20);
                rank.fontSize = 20;
                var slots = data.FindProperty("_skillSlots"); slots.arraySize = 5;
                var paths = data.FindProperty("_skillPaths"); paths.arraySize = 5;
                Vector2[] positions = { new(-220, 110), new(0, 110), new(-220, -150), new(0, -150), new(220, -150) };
                for (int i = 0; i < 5; i++)
                {
                    var slot = graph.Find("Slot" + i) ?? RectObject("Slot" + i, graph, new Vector2(154, 176), positions[i]).transform;
                    slots.GetArrayElementAtIndex(i).objectReferenceValue = slot;
                    var curve = RectObject("Path" + i, art, new Vector2(880, 500), Vector2.zero).AddComponent<UISkillTreeCurve>();
                    curve.raycastTarget = false; paths.GetArrayElementAtIndex(i).objectReferenceValue = curve;
                }
                var hubs = data.FindProperty("_skillHubs"); hubs.arraySize = 2;
                hubs.GetArrayElementAtIndex(0).objectReferenceValue = (RectTransform)Text(art, "Explore", "탐색", new Vector2(200, 36), new Vector2(-110, 235), 24).transform;
                hubs.GetArrayElementAtIndex(1).objectReferenceValue = (RectTransform)Text(art, "Support", "도움 / 하나 선택", new Vector2(320, 36), new Vector2(0, -25), 24).transform;
                var hint = Text(art, "Hint", "각 도움은 따로 강화할 수 있어요", new Vector2(700, 32), new Vector2(0, -265), 20);
                hint.color = new Color(.62f, .7f, .78f);
                data.FindProperty("_skillFlowMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/UI/SkillTreeFlow.mat");
                data.ApplyModifiedPropertiesWithoutUndo();
                ApplySceneSkin(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>기존 펫 데이터와 노드 연결을 유지하며 전체 화면과 공용 스킨을 적용한다.</summary>
        [UPlaygroundTool("PetSystem.ApplySceneSkin")]
        public static void ApplySceneSkin()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일이 끝난 Edit Mode에서 실행해야 합니다.");
            const string path = "Assets/03.Prefabs/UI/Scene/Pet/UI_Scene_PetMenu.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ApplySceneSkin(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ApplySceneSkin(GameObject root)
        {
            var theme = AssetDatabase.LoadAssetAtPath<UIVisualThemeSO>("Assets/10.Datas/UI/UIVisualTheme_Celadon.asset");
            if (theme == null) throw new InvalidOperationException("공용 청자 UI 테마가 없습니다.");
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var background = root.GetComponent<Image>();
            background.sprite = null;
            background.color = new Color(.04f, .07f, .08f, 1f);
            var frame = root.transform.Find("SceneFrame");
            if (frame == null) frame = RectObject("SceneFrame", root.transform, Vector2.zero, Vector2.zero).transform;
            var frameRect = (RectTransform)frame;
            frameRect.anchorMin = Vector2.zero; frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = frameRect.offsetMax = Vector2.zero;
            frame.SetAsFirstSibling();
            var image = frame.GetComponent<Image>() ?? frame.gameObject.AddComponent<Image>();
            image.sprite = theme.PanelFrame; image.type = Image.Type.Sliced;
            image.color = Color.white; image.raycastTarget = false;
            var title = root.transform.Find("Title").GetComponent<TextMeshProUGUI>();
            title.text = "동행 펫"; title.fontSize = theme.TitleSize;
            Place(title.transform, new Vector2(-510, 450), new Vector2(380, 60));
            title.alignment = TextAlignmentOptions.MidlineLeft;
            root.transform.Find("TitleDecoration")?.gameObject.SetActive(false);
            foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                label.color = theme.TextMain;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == "SkillTemplate") continue;
                var plate = button.GetComponent<Image>();
                plate.overrideSprite = null; plate.sprite = theme.ButtonFrame;
                plate.type = Image.Type.Sliced; plate.color = Color.white;
                plate.pixelsPerUnitMultiplier = 1f;
                var common = button.GetComponent<UICommonButton>();
                if (button.GetComponent<UITabButton>() == null)
                {
                    if (common == null) common = button.gameObject.AddComponent<UICommonButton>();
                    SetReference(common, "_button", button); SetReference(common, "_background", plate);
                    SetReference(common, "_buttonText", button.GetComponentInChildren<TextMeshProUGUI>(true));
                    SetReference(common, "_visualTarget", button.transform);
                    SetReference(common, "_theme", theme);
                    var data = new SerializedObject(common);
                    data.FindProperty("_tintBackground").boolValue = false;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    var tab = button.GetComponent<UITabButton>();
                    SetReference(tab, "_theme", theme);
                    var data = new SerializedObject(tab);
                    data.FindProperty("_tintBackground").boolValue = false;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            var card = root.transform.Find("V2SkillPage/DetailPanel").GetComponent<Image>();
            card.sprite = theme.CardFrame; card.color = Color.white; card.type = Image.Type.Sliced;
            var node = root.transform.Find("V2SkillPage/SkillList/SkillTemplate/NodeFrame").GetComponent<Image>();
            node.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/BorderFrame_Circle.png");
            var menuData = new SerializedObject(root.GetComponent<UI_Scene_PetMenu>());
            menuData.FindProperty("_layer").intValue = (int)CanvasLayer.Scene;
            menuData.FindProperty("_canCloseWithEsc").boolValue = true;
            menuData.ApplyModifiedPropertiesWithoutUndo();
            ApplyFullScreenLayout(root, theme);
        }

        private static void Place(Transform target, Vector2 position, Vector2 size)
        {
            if (target == null) return;
            var rect = (RectTransform)target;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
    }
}
