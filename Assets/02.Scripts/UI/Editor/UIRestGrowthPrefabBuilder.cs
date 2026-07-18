#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Party;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround.UI.Growth.EditorTools
{
    /// <summary>
    /// 휴식지점 성장 UI 프리팹 초안을 순수 uGUI로 생성하고 SerializeField 및 UI DB를 자동 연결한다.
    /// 재실행 가능하며 기존 프리팹의 루트 컴포넌트는 유지하고 자식 계층만 재구성한다.
    /// </summary>
    public static class UIRestGrowthPrefabBuilder
    {
        private const string PrefabPath = "Assets/03.Prefabs/UI/Scene/Growth/UI_RestGrowth.prefab";
        private const string DatabasePath = "Assets/10.Datas/Path/UIPrefabDatabase.asset";

        private static readonly Color Dim = new(0.01f, 0.02f, 0.04f, 0.84f);
        private static readonly Color PanelBg = new(0.075f, 0.095f, 0.125f, 0.99f);
        private static readonly Color CardBg = new(0.12f, 0.15f, 0.19f, 1f);
        private static readonly Color ButtonBg = new(0.20f, 0.38f, 0.46f, 1f);
        private static readonly Color TextMain = new(0.92f, 0.94f, 0.97f, 1f);
        private static readonly Color TextSub = new(0.65f, 0.70f, 0.77f, 1f);
        private static readonly Color Gold = new(0.93f, 0.76f, 0.38f, 1f);
        private static readonly Color Accent = new(0.48f, 0.82f, 1f, 1f);
        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        [MenuItem("UPlayGround/UI/프리팹 빌드/휴식 성장 (초안)")]
        public static void Build()
        {
            EnsureFolder("Assets/03.Prefabs/UI/Scene/Growth");
            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            GameObject root = prefabExists
                ? PrefabUtility.LoadPrefabContents(PrefabPath)
                : CreateRoot();

            try
            {
                UI_RestGrowth ui = root.GetComponent<UI_RestGrowth>() ?? root.AddComponent<UI_RestGrowth>();
                NormalizeRoot(root);
                ClearChildren(root.transform);
                BuildHierarchy(root, ui);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                if (prefabExists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            RegisterDatabase(prefab);
            ConfigureGrowthAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = prefab;
            Debug.Log("[RestGrowthBuilder] uGUI 프리팹, 직렬화 필드, UI DB, 기본 성장 규칙 구성을 완료했습니다.");
        }

        private static GameObject CreateRoot()
        {
            var root = new GameObject("UI_RestGrowth", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(UI_RestGrowth));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2000;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return root;
        }

        private static void NormalizeRoot(GameObject root)
        {
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Canvas canvas = root.GetComponent<Canvas>() ?? root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2000;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>() ?? root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            if (root.GetComponent<GraphicRaycaster>() == null) root.AddComponent<GraphicRaycaster>();
        }

        private static void BuildHierarchy(GameObject root, UI_RestGrowth ui)
        {
            GameObject dim = NewUI("Dim", root.transform);
            Stretch(dim);
            AddImage(dim, Dim);

            GameObject panel = NewUI("Panel", root.transform);
            Center(Rt(panel), 1180f, 820f);
            AddImage(panel, PanelBg, UISprite, true);
            VerticalLayoutGroup panelLayout = AddVLG(panel, 14f, 30);
            panelLayout.childForceExpandHeight = false;

            GameObject header = NewUI("Header", panel.transform);
            SetHeight(header, 72f);
            HorizontalLayoutGroup headerLayout = AddHLG(header, 14f, 0);
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            TextMeshProUGUI characterName = AddText(NewUI("CharacterName", header.transform), "캐릭터 성장", 36f, Gold, TextAlignmentOptions.Left);
            AddFlexibleW(characterName.gameObject, 1f);
            TextMeshProUGUI points = AddText(NewUI("GrowthPoints", header.transform), "사용 가능 포인트  0", 24f, Accent, TextAlignmentOptions.Center);
            SetWidth(points.gameObject, 290f);
            Button closeButton = MakeButton("CloseButton", header.transform, "닫기", 110f, 50f);

            TextMeshProUGUI guide = AddText(NewUI("Guide", panel.transform),
                "휴식 중에만 성장 포인트를 투자할 수 있습니다. 특정 랭크에서 콤보와 스킬이 해금됩니다.",
                21f, TextSub, TextAlignmentOptions.Center);
            SetHeight(guide.gameObject, 42f);

            GameObject cardRoot = NewUI("GrowthCards", panel.transform);
            AddFlexibleH(cardRoot, 1f);
            VerticalLayoutGroup cardLayout = AddVLG(cardRoot, 10f, 0);
            cardLayout.childForceExpandHeight = false;

            GrowthAttributeType[] attributes =
            {
                GrowthAttributeType.Health,
                GrowthAttributeType.Defense,
                GrowthAttributeType.Critical,
                GrowthAttributeType.AttackSpeed,
                GrowthAttributeType.AttackPower,
            };

            var cardRefs = new List<CardRefs>(attributes.Length);
            for (int i = 0; i < attributes.Length; i++)
                cardRefs.Add(BuildCard(cardRoot.transform, attributes[i]));

            TextMeshProUGUI unlock = AddText(NewUI("UnlockMessage", panel.transform), string.Empty, 25f, Gold, TextAlignmentOptions.Center);
            SetHeight(unlock.gameObject, 44f);

            var so = new SerializedObject(ui);
            SetRef(so, "_characterNameText", characterName);
            SetRef(so, "_pointText", points);
            SetRef(so, "_closeButton", closeButton);
            SetRef(so, "_unlockText", unlock);
            SerializedProperty cards = so.FindProperty("_cards");
            cards.arraySize = cardRefs.Count;
            for (int i = 0; i < cardRefs.Count; i++) BindCard(cards.GetArrayElementAtIndex(i), cardRefs[i]);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private readonly struct CardRefs
        {
            public readonly GrowthAttributeType Attribute;
            public readonly TextMeshProUGUI Name;
            public readonly TextMeshProUGUI Rank;
            public readonly TextMeshProUGUI Effect;
            public readonly TextMeshProUGUI Milestone;
            public readonly Button Button;

            public CardRefs(GrowthAttributeType attribute, TextMeshProUGUI name, TextMeshProUGUI rank,
                TextMeshProUGUI effect, TextMeshProUGUI milestone, Button button)
            {
                Attribute = attribute;
                Name = name;
                Rank = rank;
                Effect = effect;
                Milestone = milestone;
                Button = button;
            }
        }

        private static CardRefs BuildCard(Transform parent, GrowthAttributeType attribute)
        {
            GameObject card = NewUI(attribute + "Card", parent);
            SetHeight(card, 112f);
            AddImage(card, CardBg, UISprite, true);
            HorizontalLayoutGroup layout = AddHLG(card, 18f, 18);
            layout.childAlignment = TextAnchor.MiddleLeft;

            TextMeshProUGUI name = AddText(NewUI("Name", card.transform), DisplayName(attribute), 27f, TextMain, TextAlignmentOptions.Left);
            SetWidth(name.gameObject, 140f);
            TextMeshProUGUI rank = AddText(NewUI("Rank", card.transform), "0 / 20", 23f, Accent, TextAlignmentOptions.Center);
            SetWidth(rank.gameObject, 240f);
            rank.enableAutoSizing = true;
            rank.fontSizeMin = 17f;
            rank.fontSizeMax = 23f;
            TextMeshProUGUI effect = AddText(NewUI("Effect", card.transform), "랭크당 증가", 20f, TextSub, TextAlignmentOptions.Left);
            SetWidth(effect.gameObject, 160f);
            TextMeshProUGUI milestone = AddText(NewUI("Milestone", card.transform), "다음 해금", 19f, Gold, TextAlignmentOptions.Left);
            AddFlexibleW(milestone.gameObject, 1f);
            Button button = MakeButton("InvestButton", card.transform, "강화", 130f, 54f);
            return new CardRefs(attribute, name, rank, effect, milestone, button);
        }

        private static void BindCard(SerializedProperty property, CardRefs card)
        {
            property.FindPropertyRelative("attribute").enumValueIndex = (int)card.Attribute;
            property.FindPropertyRelative("nameText").objectReferenceValue = card.Name;
            property.FindPropertyRelative("rankText").objectReferenceValue = card.Rank;
            property.FindPropertyRelative("effectText").objectReferenceValue = card.Effect;
            property.FindPropertyRelative("milestoneText").objectReferenceValue = card.Milestone;
            property.FindPropertyRelative("investButton").objectReferenceValue = card.Button;
        }

        private static void RegisterDatabase(GameObject prefab)
        {
            UIPrefabDatabase database = AssetDatabase.LoadAssetAtPath<UIPrefabDatabase>(DatabasePath);
            if (database == null) { Debug.LogError($"[RestGrowthBuilder] UI DB 없음: {DatabasePath}"); return; }
            var so = new SerializedObject(database);
            SerializedProperty entries = so.FindProperty("prefabs");
            SerializedProperty target = null;
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("key").stringValue == "RestGrowth") { target = entry; break; }
            }
            if (target == null)
            {
                entries.InsertArrayElementAtIndex(entries.arraySize);
                target = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            }
            target.FindPropertyRelative("key").stringValue = "RestGrowth";
            target.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            // CanvasLayer의 기반 값은 0/1000/2000/... 이지만 enumValueIndex는 선언 순서(0~4)를 요구한다.
            target.FindPropertyRelative("defaultLayer").enumValueIndex =
                System.Array.IndexOf(System.Enum.GetValues(typeof(CanvasLayer)), CanvasLayer.Popup);
            target.FindPropertyRelative("description").stringValue = "휴식지점 능력치 선택 성장 UI (uGUI)";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static void ConfigureGrowthAssets()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:PartyMemberGrowthSO"))
            {
                PartyMemberGrowthSO growth = AssetDatabase.LoadAssetAtPath<PartyMemberGrowthSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (growth == null || growth.investmentRules.Count > 0) continue;
                foreach (GrowthAttributeType attribute in System.Enum.GetValues(typeof(GrowthAttributeType)))
                {
                    GrowthInvestmentRule rule = PartyMemberGrowthSO.GetDefaultInvestmentRule(attribute);
                    rule.milestones = RecommendedMilestones(attribute);
                    growth.investmentRules.Add(rule);
                }
                growth.useAutomaticLevelGrowth = false;
                growth.growthPointsPerLevel = 1;
                EditorUtility.SetDirty(growth);
            }
        }

        private static List<GrowthUnlockMilestone> RecommendedMilestones(GrowthAttributeType attribute)
        {
            var result = new List<GrowthUnlockMilestone>();
            switch (attribute)
            {
                case GrowthAttributeType.AttackPower: result.Add(Milestone(3, GrowthUnlockType.Combo, "Combo.Light.3", "약공격 3연계")); break;
                case GrowthAttributeType.AttackSpeed: result.Add(Milestone(3, GrowthUnlockType.Combo, "Combo.Heavy.2", "강공격 2연계")); break;
                case GrowthAttributeType.Defense: result.Add(Milestone(5, GrowthUnlockType.Combo, "Combo.Heavy.3", "강공격 3연계")); break;
                case GrowthAttributeType.Critical: result.Add(Milestone(5, GrowthUnlockType.Skill, "Skill.Ability", "어빌리티 스킬")); break;
                case GrowthAttributeType.Health: result.Add(Milestone(7, GrowthUnlockType.Skill, "Skill.Ultimate", "궁극 스킬")); break;
            }
            return result;
        }

        private static GrowthUnlockMilestone Milestone(int rank, GrowthUnlockType type, string id, string name) => new()
        {
            requiredRank = rank, unlockType = type, unlockId = id, displayName = name, description = $"{rank}랭크 달성 보상"
        };

        private static string DisplayName(GrowthAttributeType attribute) => attribute switch
        {
            GrowthAttributeType.Health => "체력",
            GrowthAttributeType.Defense => "방어력",
            GrowthAttributeType.Critical => "크리티컬",
            GrowthAttributeType.AttackSpeed => "공격속도",
            _ => "공격력",
        };

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform Rt(GameObject go) => go.GetComponent<RectTransform>();
        private static void Stretch(GameObject go) { RectTransform rt = Rt(go); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        private static void Center(RectTransform rt, float width, float height) { rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(width, height); rt.anchoredPosition = Vector2.zero; }
        private static Image AddImage(GameObject go, Color color, Sprite sprite = null, bool sliced = false) { Image image = go.AddComponent<Image>(); image.color = color; if (sprite != null) { image.sprite = sprite; image.type = sliced ? Image.Type.Sliced : Image.Type.Simple; } return image; }
        private static TextMeshProUGUI AddText(GameObject go, string text, float size, Color color, TextAlignmentOptions alignment) { TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>(); label.text = text; label.fontSize = size; label.color = color; label.alignment = alignment; label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Ellipsis; if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset; return label; }
        private static Button MakeButton(string name, Transform parent, string text, float width, float height) { GameObject go = NewUI(name, parent); SetWidth(go, width); SetHeight(go, height); Image image = AddImage(go, ButtonBg, UISprite, true); Button button = go.AddComponent<Button>(); button.targetGraphic = image; TextMeshProUGUI label = AddText(NewUI("Label", go.transform), text, 22f, TextMain, TextAlignmentOptions.Center); label.raycastTarget = false; Stretch(label.gameObject); return button; }
        private static VerticalLayoutGroup AddVLG(GameObject go, float spacing, int pad) { VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>(); layout.spacing = spacing; layout.padding = new RectOffset(pad, pad, pad, pad); layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false; return layout; }
        private static HorizontalLayoutGroup AddHLG(GameObject go, float spacing, int pad) { HorizontalLayoutGroup layout = go.AddComponent<HorizontalLayoutGroup>(); layout.spacing = spacing; layout.padding = new RectOffset(pad, pad, pad, pad); layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = false; layout.childForceExpandHeight = true; return layout; }
        private static void SetHeight(GameObject go, float value) { LayoutElement e = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>(); e.minHeight = e.preferredHeight = value; e.flexibleHeight = 0f; }
        private static void SetWidth(GameObject go, float value) { LayoutElement e = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>(); e.minWidth = e.preferredWidth = value; e.flexibleWidth = 0f; }
        private static void AddFlexibleW(GameObject go, float value) { LayoutElement e = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>(); e.flexibleWidth = value; }
        private static void AddFlexibleH(GameObject go, float value) { LayoutElement e = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>(); e.flexibleHeight = value; }
        private static void SetRef(SerializedObject so, string name, Object value) { SerializedProperty p = so.FindProperty(name); if (p != null) p.objectReferenceValue = value; else Debug.LogWarning($"[RestGrowthBuilder] 프로퍼티 없음: {name}"); }
        private static void ClearChildren(Transform root) { for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject); }
        private static void EnsureFolder(string path) { string[] parts = path.Split('/'); string current = parts[0]; for (int i = 1; i < parts.Length; i++) { string next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; } }
    }
}
#endif
