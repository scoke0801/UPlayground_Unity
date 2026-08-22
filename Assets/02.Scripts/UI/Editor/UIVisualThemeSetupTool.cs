#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI.EditorTools
{
    /// <summary>공용 UI 테마를 생성하고 화면·팝업의 표준 구조와 안전 영역까지 일괄 연결한다.</summary>
    public static class UIVisualThemeSetupTool
    {
        public const string ThemePath = "Assets/10.Datas/UI/UIVisualTheme.asset";
        public const string RequestPath = "Temp/UIVisualThemeSetup.request";

        private const string CommonButtonPath =
            "Assets/03.Prefabs/UI/Common/UICommonButton.prefab";
        private const string UIRootPath = "Assets/03.Prefabs/UI/UIRoot.prefab";
        private const string PanelSpritePath =
            "Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/BorderFrame_Square02_Bg.png";
        private const string ButtonSpritePath =
            "Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Button/Button_Rectangle_01_Convex_Dark.Png";
        private const string TabFocusSpritePath =
            "Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/TabMenu_01_Focus_Yellow.png";
        private const string CardSpritePath =
            "Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/Listframe_01~02_Bg.png";
        private const string SlotFocusSpritePath =
            "Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/ItemFrame_03_Focus_Yellow.png";

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/UI/비주얼 테마 생성 및 공용 UI 적용")]
        public static void CreateAndApply()
        {
            UIVisualThemeSO theme = EnsureTheme();
            ApplyToCommonButton(theme);
            ApplyToUIRoot(theme);
            ApplyToScreenPrefabs(theme);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UIVisualTheme] 테마·표준 구조·안전 영역 연결 완료: {ThemePath}");
        }

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/UI/UI UX 단계별 개선 일괄 적용")]
        public static void RunFullUpgrade()
        {
            CreateAndApply();
            if (!ValidateVisualContracts(logResult: true))
                throw new System.InvalidOperationException(
                    "UI/UX 개선 적용 후 계약 검증에 실패했습니다. Unity 콘솔을 확인하세요.");

            Debug.Log("[UIUXUpgrade] 단계별 개선 완료");
        }

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/UI/UI UX 비주얼 계약 검증")]
        public static void ValidateVisualContractsMenu()
        {
            ValidateVisualContracts(logResult: true);
        }

        [InitializeOnLoadMethod]
        private static void RunRequestedSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(RequestPath))
                    return;

                File.Delete(RequestPath);
                CreateAndApply();
            };
        }

        private static UIVisualThemeSO EnsureTheme()
        {
            EnsureFolder("Assets/10.Datas/UI");
            UIVisualThemeSO theme = AssetDatabase.LoadAssetAtPath<UIVisualThemeSO>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UIVisualThemeSO>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            var serialized = new SerializedObject(theme);
            SetSprite(serialized, "_panelFrame", PanelSpritePath);
            SetSprite(serialized, "_buttonFrame", ButtonSpritePath);
            SetSprite(serialized, "_tabFocusFrame", TabFocusSpritePath);
            SetSprite(serialized, "_cardFrame", CardSpritePath);
            SetSprite(serialized, "_slotFocusFrame", SlotFocusSpritePath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            return theme;
        }

        public static bool ValidateVisualContracts(bool logResult)
        {
            var errors = new List<string>();
            UIVisualThemeSO theme = AssetDatabase.LoadAssetAtPath<UIVisualThemeSO>(ThemePath);
            if (theme == null)
            {
                errors.Add($"비주얼 테마 누락: {ThemePath}");
            }
            else
            {
                if (theme.PanelFrame == null) errors.Add("테마 PanelFrame 누락");
                if (theme.ButtonFrame == null) errors.Add("테마 ButtonFrame 누락");
                if (theme.TabFocusFrame == null) errors.Add("테마 TabFocusFrame 누락");
                if (theme.CardFrame == null) errors.Add("테마 CardFrame 누락");
                if (theme.SlotFocusFrame == null) errors.Add("테마 SlotFocusFrame 누락");
            }

            string[] guids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { "Assets/03.Prefabs/UI" });
            int checkedScreens = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null)
                    continue;

                foreach (UI_Base ui in root.GetComponentsInChildren<UI_Base>(true))
                {
                    checkedScreens++;
                    ValidateScreen(ui, path, errors);
                }
            }

            bool valid = errors.Count == 0;
            if (logResult)
            {
                string body = errors.Count == 0
                    ? "오류 없음"
                    : string.Join("\n", errors.ConvertAll(error => $"- {error}"));
                if (valid)
                    Debug.Log($"[UIVisualTheme] 비주얼 계약 검증 성공: 화면 {checkedScreens}개\n{body}");
                else
                    Debug.LogError($"[UIVisualTheme] 비주얼 계약 검증 실패: {errors.Count}건\n{body}");
            }

            return valid;
        }

        private static void ValidateScreen(UI_Base ui, string path, List<string> errors)
        {
            var serialized = new SerializedObject(ui);
            CanvasLayer layer = ReadLayer(serialized);
            RectTransform transitionTarget = null;

            if (ui is UI_SceneBase)
            {
                transitionTarget = GetReference<RectTransform>(serialized, "_sceneContent");
                if (transitionTarget == null)
                    errors.Add($"Scene 콘텐츠 참조 누락: {path} ({ui.GetType().Name})");
                if (layer != CanvasLayer.Scene)
                    errors.Add($"Scene 레이어 불일치: {path} ({layer})");
            }
            else if (ui is UI_PopupBase)
            {
                transitionTarget = GetReference<RectTransform>(serialized, "_panel");
                if (transitionTarget == null)
                    errors.Add($"Popup 패널 참조 누락: {path} ({ui.GetType().Name})");
                CanvasLayer expected = ui is UI_Scene_PauseMenu
                    ? CanvasLayer.Scene
                    : CanvasLayer.Popup;
                if (layer != expected)
                    errors.Add($"Popup 레이어 불일치: {path} ({layer}, 기대 {expected})");
            }
            else if (layer == CanvasLayer.HUD && ShouldFitHud(ui))
            {
                transitionTarget = ui.transform as RectTransform;
            }

            if (transitionTarget != null
                && transitionTarget.GetComponent<UISafeAreaFitter>() == null)
            {
                errors.Add($"안전 영역 미적용: {path} ({transitionTarget.name})");
            }

            if ((ui is UI_Scene_Inventory
                 || ui is UI_Scene_QuestMenu
                 || ui is UI_Scene_MonsterCodex)
                && GetReference<UIEmptyStateView>(serialized, "_emptyState") == null)
            {
                errors.Add($"빈 상태 안내 누락: {path} ({ui.GetType().Name})");
            }

            foreach (TextMeshProUGUI text in ui.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                string value = text.text ?? string.Empty;
                if (value.Contains("ID:")
                    || value.Contains("ScriptableObject")
                    || value.Contains("에셋이 연결되지"))
                {
                    errors.Add($"플레이어 노출 개발 용어: {path}/{text.name} = '{value}'");
                }
            }
        }

        private static void ApplyToCommonButton(UIVisualThemeSO theme)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CommonButtonPath) == null)
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(CommonButtonPath);
            try
            {
                UICommonButton button = root.GetComponent<UICommonButton>();
                if (button == null)
                    return;

                var serialized = new SerializedObject(button);
                serialized.FindProperty("_theme").objectReferenceValue = theme;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(button);
                PrefabUtility.SaveAsPrefabAsset(root, CommonButtonPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyToUIRoot(UIVisualThemeSO theme)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(UIRootPath) == null)
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(UIRootPath);
            try
            {
                UIVisualThemeProvider provider = root.GetComponent<UIVisualThemeProvider>();
                if (provider == null)
                    provider = root.AddComponent<UIVisualThemeProvider>();

                var serialized = new SerializedObject(provider);
                serialized.FindProperty("_theme").objectReferenceValue = theme;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(provider);
                PrefabUtility.SaveAsPrefabAsset(root, UIRootPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyToScreenPrefabs(UIVisualThemeSO theme)
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { "Assets/03.Prefabs/UI" });
            int changedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponentInChildren<UI_Base>(true) == null)
                    continue;

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool changed = false;
                    foreach (UI_Base ui in root.GetComponentsInChildren<UI_Base>(true))
                    {
                        changed |= ApplyStandardStructure(ui, theme);
                        changed |= ApplyControlTheme(ui.gameObject, theme);
                    }

                    if (!changed)
                        continue;

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changedCount++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            Debug.Log($"[UIVisualTheme] UI 프리팹 {changedCount}개 갱신");
        }

        private static bool ApplyStandardStructure(UI_Base ui, UIVisualThemeSO theme)
        {
            bool changed = false;
            var serialized = new SerializedObject(ui);
            RectTransform safeAreaTarget = null;

            if (ui is UI_SceneBase)
            {
                RectTransform content = ResolveRect(
                    ui.transform,
                    "Content",
                    "Window",
                    "Panel",
                    "SkillTreeRuntimeRoot",
                    "ButtonPanel");
                changed |= SetReference(serialized, "_sceneContent", content);
                changed |= SetLayer(serialized, CanvasLayer.Scene);
                safeAreaTarget = content;
            }
            else if (ui is UI_PopupBase)
            {
                RectTransform panel = ResolveRect(ui.transform, "Panel", "Window", "Content");
                RectTransform dimRect = ResolveRect(ui.transform, "Dim");
                CanvasGroup dim = dimRect != null ? dimRect.GetComponent<CanvasGroup>() : null;
                if (dimRect != null && dim == null)
                {
                    dim = dimRect.gameObject.AddComponent<CanvasGroup>();
                    changed = true;
                }

                changed |= SetReference(serialized, "_panel", panel);
                changed |= SetReference(serialized, "_dim", dim);

                // 일시정지 메뉴는 Scene 레이어에서 Popup 모션만 재사용하는 의도된 예외다.
                bool isSceneOverlay = ui is UI_Scene_PauseMenu;
                changed |= SetLayer(
                    serialized,
                    isSceneOverlay ? CanvasLayer.Scene : CanvasLayer.Popup);
                safeAreaTarget = panel;
            }
            else if (ReadLayer(serialized) == CanvasLayer.HUD && ShouldFitHud(ui))
            {
                safeAreaTarget = ui.transform as RectTransform;
            }

            if (safeAreaTarget != null
                && safeAreaTarget.GetComponent<UISafeAreaFitter>() == null)
            {
                safeAreaTarget.gameObject.AddComponent<UISafeAreaFitter>();
                changed = true;
            }

            if (serialized.hasModifiedProperties)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            if (safeAreaTarget != null)
                changed |= ApplyPanelSprite(safeAreaTarget);
            changed |= EnsureEmptyState(ui, theme);
            return changed;
        }

        private static bool EnsureEmptyState(UI_Base ui, UIVisualThemeSO theme)
        {
            var serialized = new SerializedObject(ui);
            SerializedProperty emptyProperty = serialized.FindProperty("_emptyState");
            if (emptyProperty == null)
                return false;
            if (emptyProperty.objectReferenceValue != null)
                return false;

            ScrollRect scrollRect = null;
            if (ui is UI_Scene_Inventory)
            {
                scrollRect = GetReference<ScrollRect>(serialized, "_itemScrollRect");
            }
            else
            {
                Transform content = ui is UI_Scene_QuestMenu
                    ? GetReference<Transform>(serialized, "_questListContent")
                    : ui is UI_Scene_MonsterCodex
                        ? GetReference<Transform>(serialized, "_listContent")
                        : null;
                scrollRect = content != null ? content.GetComponentInParent<ScrollRect>(true) : null;
            }

            RectTransform parent = scrollRect != null
                ? scrollRect.viewport != null
                    ? scrollRect.viewport
                    : scrollRect.transform as RectTransform
                : null;
            if (parent == null)
                return false;

            UIEmptyStateView view = ui.GetComponentInChildren<UIEmptyStateView>(true);
            if (view == null)
                view = CreateEmptyState(parent, ui, theme);

            emptyProperty.objectReferenceValue = view;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static UIEmptyStateView CreateEmptyState(
            RectTransform parent,
            UI_Base owner,
            UIVisualThemeSO theme)
        {
            var root = new GameObject(
                "EmptyState",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(UIEmptyStateView));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(660f, 210f);

            Image background = root.GetComponent<Image>();
            background.sprite = theme.CardFrame;
            background.type = theme.CardFrame != null && theme.CardFrame.border.sqrMagnitude > 0f
                ? Image.Type.Sliced
                : Image.Type.Simple;
            background.color = Color.white;
            background.raycastTarget = false;

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            TMP_FontAsset font = owner.GetComponentInChildren<TextMeshProUGUI>(true)?.font;
            TextMeshProUGUI title = CreateEmptyStateText(
                root.transform,
                "Title",
                new Vector2(0.08f, 0.50f),
                new Vector2(0.92f, 0.84f),
                theme.HeadingSize,
                theme.TextMain,
                font);
            TextMeshProUGUI hint = CreateEmptyStateText(
                root.transform,
                "Hint",
                new Vector2(0.08f, 0.14f),
                new Vector2(0.92f, 0.52f),
                theme.LabelSize,
                theme.TextSub,
                font);
            UIEmptyStateView view = root.GetComponent<UIEmptyStateView>();
            view.Configure(title, hint);
            root.SetActive(false);
            return view;
        }

        private static TextMeshProUGUI CreateEmptyStateText(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float fontSize,
            Color color,
            TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static T GetReference<T>(SerializedObject serialized, string propertyName)
            where T : Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property?.objectReferenceValue as T;
        }

        private static bool ApplyControlTheme(GameObject scope, UIVisualThemeSO theme)
        {
            bool changed = false;
            foreach (UICommonButton button in scope.GetComponentsInChildren<UICommonButton>(true))
            {
                var serialized = new SerializedObject(button);
                changed |= SetReference(serialized, "_theme", theme);
                if (serialized.hasModifiedProperties)
                    serialized.ApplyModifiedPropertiesWithoutUndo();

                Button unityButton = button.GetComponent<Button>();
                Image background = unityButton != null ? unityButton.targetGraphic as Image : null;
                changed |= ApplySprite(background, theme.ButtonFrame);
            }

            foreach (UITabButton tab in scope.GetComponentsInChildren<UITabButton>(true))
            {
                var serialized = new SerializedObject(tab);
                changed |= SetReference(serialized, "_theme", theme);
                SerializedProperty indicatorProperty = serialized.FindProperty("_selectedIndicator");
                GameObject indicator = indicatorProperty?.objectReferenceValue as GameObject;
                if (serialized.hasModifiedProperties)
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                changed |= ApplySprite(indicator != null ? indicator.GetComponent<Image>() : null,
                    theme.TabFocusFrame);
            }

            return changed;
        }

        private static bool ApplyPanelSprite(RectTransform target)
        {
            UIVisualThemeSO theme = AssetDatabase.LoadAssetAtPath<UIVisualThemeSO>(ThemePath);
            return ApplySprite(target != null ? target.GetComponent<Image>() : null,
                theme != null ? theme.PanelFrame : null);
        }

        private static bool ApplySprite(Image image, Sprite sprite)
        {
            if (image == null || sprite == null || image.sprite == sprite)
                return false;

            image.sprite = sprite;
            if (sprite.border.sqrMagnitude > 0f)
                image.type = Image.Type.Sliced;
            EditorUtility.SetDirty(image);
            return true;
        }

        /// <summary>
        /// 화면의 콘텐츠 루트를 이름으로 찾는다. 얕은 후보를 먼저 고르고,
        /// 레이아웃 그룹이 위치를 통제하는 자식은 후보에서 제외한다.
        /// 코너 패널 안의 목록 컨테이너("Content")까지 집어 들면 SafeArea 보정이
        /// 레이아웃과 충돌해 그 목록이 화면 밖으로 밀려나기 때문이다.
        /// </summary>
        private static RectTransform ResolveRect(Transform scope, params string[] names)
        {
            RectTransform[] children = scope.GetComponentsInChildren<RectTransform>(true);
            foreach (string name in names)
            {
                RectTransform best = null;
                int bestDepth = int.MaxValue;

                foreach (RectTransform child in children)
                {
                    if (child == scope
                        || !child.name.StartsWith(name, System.StringComparison.OrdinalIgnoreCase)
                        || IsLayoutControlled(child))
                    {
                        continue;
                    }

                    int depth = GetDepth(child, scope);
                    if (depth >= bestDepth)
                        continue;

                    best = child;
                    bestDepth = depth;
                }

                if (best != null)
                    return best;
            }

            return null;
        }

        /// <summary>부모의 LayoutGroup이 이 RectTransform의 앵커·크기를 통제하는지 여부.</summary>
        private static bool IsLayoutControlled(RectTransform rect)
            => rect.parent != null && rect.parent.GetComponent<LayoutGroup>() != null;

        private static int GetDepth(Transform child, Transform scope)
        {
            int depth = 0;
            for (Transform cursor = child; cursor != null && cursor != scope; cursor = cursor.parent)
                depth++;
            return depth;
        }

        private static bool SetReference(
            SerializedObject serialized,
            string propertyName,
            Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value)
                return false;
            property.objectReferenceValue = value;
            return true;
        }

        private static bool SetLayer(SerializedObject serialized, CanvasLayer layer)
        {
            SerializedProperty property = serialized.FindProperty("_layer");
            if (property == null || property.intValue == (int)layer)
                return false;
            property.intValue = (int)layer;
            return true;
        }

        private static CanvasLayer ReadLayer(SerializedObject serialized)
        {
            SerializedProperty property = serialized.FindProperty("_layer");
            return property != null ? (CanvasLayer)property.intValue : CanvasLayer.Scene;
        }

        private static bool ShouldFitHud(UI_Base ui)
        {
            string typeName = ui.GetType().Name;
            return typeName.IndexOf("Entry", System.StringComparison.Ordinal) < 0
                   && typeName.IndexOf("Interaction", System.StringComparison.Ordinal) < 0;
        }

        private static void SetSprite(
            SerializedObject serialized,
            string propertyName,
            string assetPath)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
