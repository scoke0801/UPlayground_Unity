#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.EditorTools
{
    /// <summary>인게임 메뉴 패널의 확장 타일과 레이아웃을 일괄 갱신한다.</summary>
    public static class UIMenuPanelPrefabBuilder
    {
        public const string PrefabPath = "Assets/03.Prefabs/UI/HUD/UI_Scene_MenuPanel.prefab";

        private const string CodexIconPath =
            "Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/IconMisc/MenuIcon_Monster.png";
        private const string SkillTreeIconPath =
            "Assets/ExternalAssets/UI/Artsystack - Fantasy RPG GUI/ResourcesData/Sprites/components/skill_tree.png";

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/UI/메뉴 패널 프리팹 갱신")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                throw new System.InvalidOperationException($"메뉴 패널 프리팹 없음: {PrefabPath}");

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                UI_Scene_MenuPanel panel = root.GetComponent<UI_Scene_MenuPanel>();
                if (panel == null)
                    throw new System.InvalidOperationException("UI_Scene_MenuPanel 컴포넌트가 없습니다.");

                var serialized = new SerializedObject(panel);
                Button partyButton = serialized.FindProperty("_partyButton")?.objectReferenceValue as Button;
                Transform partySlot = partyButton != null ? partyButton.transform.parent : null;
                Transform gridRoot = partySlot != null ? partySlot.parent : null;
                GridLayoutGroup grid = gridRoot != null ? gridRoot.GetComponent<GridLayoutGroup>() : null;
                if (partySlot == null || grid == null)
                    throw new System.InvalidOperationException("파티 메뉴 슬롯 또는 메뉴 GridLayoutGroup을 찾지 못했습니다.");

                EnsureSlot(serialized, gridRoot, partySlot,
                    "_codexButton", "CodexSlot", "CodexButton", "도감", CodexIconPath);
                EnsureSlot(serialized, gridRoot, partySlot,
                    "_skillTreeButton", "SkillTreeSlot", "SkillTreeButton", "스킬 트리", SkillTreeIconPath);
                ConfigureGrid(grid);

                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UIMenuPanelPrefabBuilder] 도감·스킬 트리 메뉴 타일과 4x2 배치 갱신 완료");
        }

        private static void EnsureSlot(
            SerializedObject serialized,
            Transform gridRoot,
            Transform templateSlot,
            string fieldName,
            string slotName,
            string buttonName,
            string labelText,
            string iconPath)
        {
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
                throw new System.InvalidOperationException($"UI_Scene_MenuPanel 직렬화 필드 없음: {fieldName}");

            Button current = property.objectReferenceValue as Button;
            if (current != null && current.transform.parent?.parent != gridRoot)
            {
                Object.DestroyImmediate(current.gameObject);
                property.objectReferenceValue = null;
            }

            Transform slot = gridRoot.Find(slotName);
            if (slot == null)
            {
                GameObject clone = Object.Instantiate(templateSlot.gameObject, gridRoot);
                clone.name = slotName;
                slot = clone.transform;
            }

            Button button = slot.GetComponentInChildren<Button>(true);
            if (button == null)
                throw new System.InvalidOperationException($"{slotName}에 Button이 없습니다.");

            button.gameObject.name = buttonName;
            foreach (TextMeshProUGUI label in slot.GetComponentsInChildren<TextMeshProUGUI>(true))
                label.text = labelText;

            Image icon = button.transform.childCount > 0
                ? button.transform.GetChild(0).GetComponent<Image>()
                : null;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (icon == null)
                throw new System.InvalidOperationException($"{buttonName} 아이콘 Image가 없습니다.");
            if (sprite == null)
                throw new System.InvalidOperationException($"메뉴 아이콘을 찾지 못했습니다: {iconPath}");

            icon.sprite = sprite;
            property.objectReferenceValue = button;
        }

        private static void ConfigureGrid(GridLayoutGroup grid)
        {
            grid.enabled = false;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = new Vector2(220f, 270f);
            grid.spacing = new Vector2(0f, 10f);
            grid.padding = new RectOffset(10, 10, 20, 0);

            var slots = new System.Collections.Generic.List<RectTransform>();
            foreach (Transform child in grid.transform)
            {
                Button button = child.GetComponentInChildren<Button>(true);
                if (button == null || child is not RectTransform slot)
                    continue;

                slots.Add(slot);
                if (button.transform is RectTransform buttonRect)
                    buttonRect.sizeDelta = new Vector2(200f, 200f);
                if (button.transform.childCount > 0 && button.transform.GetChild(0) is RectTransform iconRect)
                {
                    iconRect.sizeDelta = new Vector2(130f, 130f);
                    iconRect.anchoredPosition = new Vector2(0f, 12f);
                }
            }

            for (int index = 0; index < slots.Count; index++)
            {
                bool secondRow = index >= 4;
                int column = secondRow ? index - 4 : index;
                int columnCount = secondRow ? Mathf.Max(1, slots.Count - 4) : Mathf.Min(4, slots.Count);
                RectTransform slot = slots[index];
                slot.anchorMin = slot.anchorMax = new Vector2((column + 0.5f) / columnCount, 1f);
                slot.pivot = new Vector2(0.5f, 1f);
                slot.anchoredPosition = new Vector2(0f, secondRow ? -300f : -20f);
                slot.sizeDelta = new Vector2(220f, 270f);
                slot.localScale = Vector3.one;
            }
        }
    }
}
#endif
