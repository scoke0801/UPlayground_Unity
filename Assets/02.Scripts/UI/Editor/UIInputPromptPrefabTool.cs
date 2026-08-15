using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UPlayGround.InputDefine;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.UI.EditorTools
{
    /// <summary>
    /// 장치 반응형 입력 프롬프트를 기존 UI 프리팹에 비파괴적으로 반영하고 계약을 검증한다.
    /// 프리팹 루트/기존 컴포넌트/직렬화 참조는 유지하고 프롬프트 관련 자식만 수정한다.
    /// </summary>
    public static class UIInputPromptPrefabTool
    {
        public const string RequestFile = "Temp/UIInputPromptMigration.request";
        public const string ResultFile = "Temp/UIInputPromptValidation.log";

        private const string InputActionsPath =
            "Assets/Resources/Input/PlayerInputActions.inputactions";
        private const string InventoryPrefabPath =
            "Assets/03.Prefabs/UI/Scene/Inventory/UI_Scene_Inventory.prefab";

        private enum Placement
        {
            Layout,
            BottomOverlay,
        }

        private sealed class PrefabPromptDefinition
        {
            public readonly string PrefabPath;
            public readonly string ParentPath;
            public readonly string BarName;
            public readonly Placement Placement;
            public readonly float Width;
            public readonly UIInputPromptBarBuilderUtility.PromptSpec[] Prompts;

            public PrefabPromptDefinition(
                string prefabPath,
                string parentPath,
                string barName,
                Placement placement,
                float width,
                params UIInputPromptBarBuilderUtility.PromptSpec[] prompts)
            {
                PrefabPath = prefabPath;
                ParentPath = parentPath;
                BarName = barName;
                Placement = placement;
                Width = width;
                Prompts = prompts;
            }
        }

        private static UIInputPromptBarBuilderUtility.PromptSpec Prompt(
            string action,
            string label)
            => new(action, label);

        private static readonly PrefabPromptDefinition[] Definitions =
        {
            new(
                InventoryPrefabPath,
                "Window/Header",
                "NavigationPromptBar",
                Placement.Layout,
                0f,
                Prompt(UIAction.MainTabPrevious, "이전 메뉴"),
                Prompt(UIAction.MainTabNext, "다음 메뉴"),
                Prompt(UIAction.SubTabPrevious, "이전 분류"),
                Prompt(UIAction.SubTabNext, "다음 분류"),
                Prompt(UIAction.Submit, "확인"),
                Prompt(UIAction.Cancel, "뒤로")),
            new(
                "Assets/03.Prefabs/UI/Scene/Craft/UI_Scene_CraftMenu.prefab",
                "Window/Header",
                "NavigationPromptBar",
                Placement.Layout,
                0f,
                Prompt(UIAction.MainTabPrevious, "이전 메뉴"),
                Prompt(UIAction.MainTabNext, "다음 메뉴"),
                Prompt(UIAction.SubTabPrevious, "이전 분류"),
                Prompt(UIAction.SubTabNext, "다음 분류"),
                Prompt(UIAction.Submit, "확인"),
                Prompt(UIAction.Cancel, "뒤로")),
            new(
                "Assets/03.Prefabs/UI/Scene/Quest/UI_Scene_QuestMenu.prefab",
                "Window/Header",
                "NavigationPromptBar",
                Placement.Layout,
                0f,
                Prompt(UIAction.MainTabPrevious, "이전 메뉴"),
                Prompt(UIAction.MainTabNext, "다음 메뉴"),
                Prompt(UIAction.SubTabPrevious, "이전 상태"),
                Prompt(UIAction.SubTabNext, "다음 상태"),
                Prompt(UIAction.Submit, "확인"),
                Prompt(UIAction.Cancel, "뒤로")),
            new(
                "Assets/03.Prefabs/UI/Scene/UI_Scene_SettingMenu.prefab",
                "Panel/Footer",
                "NavigationPromptBar",
                Placement.Layout,
                0f,
                Prompt(UIAction.MainTabPrevious, "이전 메뉴"),
                Prompt(UIAction.MainTabNext, "다음 메뉴"),
                Prompt(UIAction.SubTabPrevious, "이전 설정 탭"),
                Prompt(UIAction.SubTabNext, "다음 설정 탭"),
                Prompt(UIAction.Submit, "확인"),
                Prompt(UIAction.Cancel, "뒤로")),
            new(
                "Assets/03.Prefabs/UI/Scene/UI_Scene_SaveSlotMenu.prefab",
                "Panel/Footer",
                "CommonPromptBar",
                Placement.Layout,
                0f,
                Prompt(UIAction.Submit, "선택"),
                Prompt(UIAction.Cancel, "닫기")),
            new(
                "Assets/03.Prefabs/UI/Scene/Map/UI_Scene_Map.prefab",
                string.Empty,
                "MapPromptBar",
                Placement.BottomOverlay,
                900f,
                Prompt(UIAction.MainTabPrevious, "이전 메뉴"),
                Prompt(UIAction.MainTabNext, "다음 메뉴"),
                Prompt(UIAction.Submit, "마커 선택"),
                Prompt(UIAction.Cancel, "닫기")),
            new(
                "Assets/03.Prefabs/UI/Scene/Codex/UI_Scene_MonsterCodex.prefab",
                "Window/Header",
                "MainNavigationPromptBar",
                Placement.Layout,
                0f,
                Prompt(UIAction.MainTabPrevious, "이전 메뉴"),
                Prompt(UIAction.MainTabNext, "다음 메뉴"),
                Prompt(UIAction.Submit, "확인"),
                Prompt(UIAction.Cancel, "뒤로")),
            new(
                "Assets/03.Prefabs/UI/Scene/CharacterSelect/UI_Scene_CharacterSelect.prefab",
                string.Empty,
                "CommonPromptBar",
                Placement.BottomOverlay,
                520f,
                Prompt(UIAction.Submit, "시작"),
                Prompt(UIAction.Cancel, "취소")),
            new(
                "Assets/03.Prefabs/UI/Scene/UI_Scene_PauseMenu.prefab",
                "Panel",
                "CommonPromptBar",
                Placement.Layout,
                0f,
                Prompt(UIAction.Submit, "선택"),
                Prompt(UIAction.Cancel, "재개")),
            new(
                "Assets/03.Prefabs/UI/Scene/Growth/UI_Scene_RestGrowth.prefab",
                "Panel",
                "CommonPromptBar",
                Placement.Layout,
                0f,
                Prompt(UIAction.Submit, "성장"),
                Prompt(UIAction.Cancel, "닫기")),
            new(
                "Assets/03.Prefabs/UI/Scene/Party/UI_Scene_PartyMenu.prefab",
                string.Empty,
                "PartyPromptBar",
                Placement.BottomOverlay,
                900f,
                Prompt(UIAction.MainTabPrevious, "이전 메뉴"),
                Prompt(UIAction.MainTabNext, "다음 메뉴"),
                Prompt(UIAction.Submit, "선택"),
                Prompt(UIAction.Cancel, "닫기")),
        };

        private static readonly string[] LegacyObjectNames =
        {
            "NavigationHint",
            "FooterHint",
            "EscHint",
        };

        private static readonly Dictionary<string, string> LegacyTextReplacements =
            new(StringComparer.Ordinal)
            {
                ["ESC  닫기"] = "닫기",
                ["ESC  취소"] = "취소",
                ["Esc"] = "닫기",
            };

        [MenuItem("Tools/UI/Input Prompt/프리팹 마이그레이션")]
        public static void MigrateAllMenu()
        {
            int changed = MigrateAll();
            UIInputPromptValidationReport report = ValidateAll(logResult: true);
            Debug.Log($"[InputPromptTool] 프리팹 {changed}개 반영 완료.\n{report}");
        }

        [MenuItem("Tools/UI/Input Prompt/전체 계약 검증")]
        public static void ValidateAllMenu()
        {
            ValidateAll(logResult: true);
        }

        [MenuItem("Tools/UI/Input Prompt/자동 실행 요청 생성")]
        public static void CreateMigrationRequest()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RequestFile) ?? "Temp");
            File.WriteAllText(RequestFile, DateTime.UtcNow.ToString("O"));
            Debug.Log($"[InputPromptTool] 다음 스크립트 리로드에서 마이그레이션을 실행합니다: {RequestFile}");
        }

        [InitializeOnLoadMethod]
        private static void ScheduleRequestedMigration()
        {
            EditorApplication.delayCall -= RunRequestedMigration;
            EditorApplication.delayCall += RunRequestedMigration;
        }

        private static void RunRequestedMigration()
        {
            if (!File.Exists(RequestFile))
                return;

            try
            {
                File.Delete(RequestFile);
                int changed = MigrateAll();
                UIInputPromptValidationReport report = ValidateAll(logResult: true);
                Directory.CreateDirectory(Path.GetDirectoryName(ResultFile) ?? "Temp");
                File.WriteAllText(
                    ResultFile,
                    $"changedPrefabs={changed}{Environment.NewLine}{report}");
            }
            catch (Exception exception)
            {
                File.WriteAllText(ResultFile, exception.ToString());
                Debug.LogException(exception);
            }
        }

        public static int MigrateAll()
        {
            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (PrefabPromptDefinition definition in Definitions)
                {
                    if (MigratePrefab(definition))
                        changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return changed;
        }

        private static bool MigratePrefab(PrefabPromptDefinition definition)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(definition.PrefabPath) == null)
                return false;

            GameObject root = PrefabUtility.LoadPrefabContents(definition.PrefabPath);
            try
            {
                Transform parent = string.IsNullOrWhiteSpace(definition.ParentPath)
                    ? root.transform
                    : root.transform.Find(definition.ParentPath);
                if (parent == null)
                {
                    Debug.LogError(
                        $"[InputPromptTool] 부모를 찾지 못했습니다: " +
                        $"{definition.PrefabPath} :: {definition.ParentPath}");
                    return false;
                }

                RemoveLegacyPromptObjects(root);
                ReplaceLegacyText(root);
                ConfigureInventoryScrollReference(definition.PrefabPath, root);

                UIInputPromptBar bar =
                    UIInputPromptBarBuilderUtility.FindOrAddBar(
                        parent,
                        definition.BarName,
                        42f,
                        definition.Prompts);
                ConfigurePlacement(bar, definition);
                bar.transform.SetAsLastSibling();

                PrefabUtility.SaveAsPrefabAsset(root, definition.PrefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RemoveLegacyPromptObjects(GameObject root)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = transforms.Length - 1; i >= 0; i--)
            {
                Transform candidate = transforms[i];
                if (candidate == root.transform
                    || !LegacyObjectNames.Contains(candidate.name, StringComparer.Ordinal))
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(candidate.gameObject);
            }
        }

        private static void ReplaceLegacyText(GameObject root)
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (LegacyTextReplacements.TryGetValue(text.text, out string replacement))
                {
                    text.text = replacement;
                    EditorUtility.SetDirty(text);
                }
            }
        }

        private static void ConfigureInventoryScrollReference(
            string prefabPath,
            GameObject root)
        {
            if (!string.Equals(prefabPath, InventoryPrefabPath, StringComparison.Ordinal))
                return;

            UI_Scene_Inventory inventory = root.GetComponent<UI_Scene_Inventory>();
            if (inventory == null)
                return;

            var serialized = new SerializedObject(inventory);
            SerializedProperty contentProperty = serialized.FindProperty("_content");
            Transform content = contentProperty?.objectReferenceValue as Transform;
            SerializedProperty scrollProperty = serialized.FindProperty("_itemScrollRect");
            if (scrollProperty == null || content == null)
                return;

            scrollProperty.objectReferenceValue = content.GetComponentInParent<ScrollRect>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(inventory);
        }

        private static void ConfigurePlacement(
            UIInputPromptBar bar,
            PrefabPromptDefinition definition)
        {
            RectTransform rect = (RectTransform)bar.transform;
            if (definition.Placement == Placement.Layout)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(definition.Width, 42f);
            rect.anchoredPosition = new Vector2(0f, 18f);

            LayoutElement layout = bar.GetComponent<LayoutElement>();
            if (layout != null)
                layout.ignoreLayout = true;
        }

        public static UIInputPromptValidationReport ValidateAll(bool logResult = false)
        {
            var report = new UIInputPromptValidationReport();
            ValidateInputActionAsset(report);
            ValidateGlyphData(report);

            foreach (PrefabPromptDefinition definition in Definitions)
                ValidatePrefab(definition, report);

            if (logResult)
            {
                if (report.IsValid)
                    Debug.Log($"[InputPromptTool] 검증 성공\n{report}");
                else
                    Debug.LogError($"[InputPromptTool] 검증 실패\n{report}");
            }

            return report;
        }

        private static void ValidateInputActionAsset(UIInputPromptValidationReport report)
        {
            InputActionAsset asset =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (asset == null)
            {
                report.AddError($"InputActionAsset 누락: {InputActionsPath}");
                return;
            }

            InputActionMap ui = asset.FindActionMap(InputMapNames.UI, false);
            if (ui == null)
            {
                report.AddError($"액션 맵 누락: {InputMapNames.UI}");
                return;
            }

            var required = new[]
            {
                (UIAction.Navigate, true, true),
                (UIAction.Submit, true, true),
                (UIAction.Cancel, true, true),
                (UIAction.MainTabPrevious, false, true),
                (UIAction.MainTabNext, false, true),
                (UIAction.SubTabPrevious, false, true),
                (UIAction.SubTabNext, false, true),
            };

            foreach ((string actionName, bool keyboardMouse, bool gamepad) in required)
            {
                InputAction action = ui.FindAction(actionName, false);
                if (action == null)
                {
                    report.AddError($"UI 액션 누락: {actionName}");
                    continue;
                }

                if (keyboardMouse
                    && !InputPromptAvailability.HasBindingFor(
                        action,
                        ActiveInputDevice.KeyboardMouse))
                {
                    report.AddError($"키보드·마우스 바인딩 누락: UI/{actionName}");
                }

                if (gamepad
                    && !InputPromptAvailability.HasBindingFor(
                        action,
                        ActiveInputDevice.Gamepad))
                {
                    report.AddError($"게임패드 바인딩 누락: UI/{actionName}");
                }
            }

            foreach (InputActionMap map in asset.actionMaps)
            {
                foreach (InputAction action in map.actions)
                {
                    foreach (InputBinding binding in action.bindings)
                    {
                        if (binding.id == Guid.Empty)
                            report.AddError($"binding GUID 누락: {map.name}/{action.name}");
                        if (string.IsNullOrWhiteSpace(binding.path))
                            report.AddError($"빈 binding path: {map.name}/{action.name}/{binding.id}");
                    }
                }
            }

            ValidatePhysicalPathCollisions(asset, report);

            string[] requiredSchemes = { "Keyboard&Mouse", "Gamepad" };
            foreach (string scheme in requiredSchemes)
            {
                if (!asset.controlSchemes.Any(value =>
                        string.Equals(value.name, scheme, StringComparison.Ordinal)))
                {
                    report.AddError($"Control Scheme 누락: {scheme}");
                }
            }
        }

        private static readonly Dictionary<string, HashSet<string>>
            AllowedPhysicalPathSharing = BuildAllowedPhysicalPathSharing();

        private static Dictionary<string, HashSet<string>> BuildAllowedPhysicalPathSharing()
        {
            var result = new Dictionary<string, HashSet<string>>(
                StringComparer.OrdinalIgnoreCase);

            void Add(string map, string path, params string[] actions)
            {
                result[$"{map}|{path}"] =
                    new HashSet<string>(actions, StringComparer.Ordinal);
            }

            Add(InputMapNames.PlayerAction, "<Gamepad>/leftShoulder",
                PlayerAction.Dodge,
                PlayerAction.SkillUltimate,
                PlayerAction.Guard,
                PlayerAction.QuickSlot_Left,
                PlayerAction.QuickSlot_Down,
                PlayerAction.QuickSlot_Right,
                PlayerAction.QuickSlot_Up);
            Add(InputMapNames.PlayerAction, "<Gamepad>/rightShoulder",
                PlayerAction.Dodge,
                PlayerAction.ElementBuff);
            Add(InputMapNames.PlayerAction, "<Gamepad>/rightTrigger",
                PlayerAction.Interact,
                PlayerAction.SkillUltimate);
            Add(InputMapNames.PlayerAction, "<Gamepad>/dpad/up",
                PlayerAction.CharacterSwap_1,
                PlayerAction.QuickSlot_Up);
            Add(InputMapNames.PlayerAction, "<Gamepad>/dpad/right",
                PlayerAction.CharacterSwap_2,
                PlayerAction.QuickSlot_Right);
            Add(InputMapNames.PlayerAction, "<Gamepad>/dpad/down",
                PlayerAction.CharacterSwap_3,
                PlayerAction.QuickSlot_Down);
            Add(InputMapNames.PlayerAction, "<Gamepad>/dpad/left",
                PlayerAction.CharacterSwap_4,
                PlayerAction.QuickSlot_Left);

            Add(InputMapNames.UI, "<Gamepad>/leftStick",
                UIAction.Navigate,
                "CursorMove");
            Add(InputMapNames.UI, "<Gamepad>/buttonSouth",
                UIAction.Submit,
                "CursorClick");
            Add(InputMapNames.UI, "<Keyboard>/space",
                UIAction.Submit,
                UIAction.DialogueNext);
            Add(InputMapNames.UI, "<Gamepad>/rightTrigger",
                UIAction.DialogueSkip,
                UIAction.MainTabNext);
            Add(InputMapNames.UI, "<Gamepad>/leftShoulder",
                UIAction.DialogueBacklog,
                UIAction.SubTabPrevious);

            return result;
        }

        private static void ValidatePhysicalPathCollisions(
            InputActionAsset asset,
            UIInputPromptValidationReport report)
        {
            foreach (InputActionMap map in asset.actionMaps)
            {
                var owners = new Dictionary<string, HashSet<string>>(
                    StringComparer.OrdinalIgnoreCase);
                foreach (InputAction action in map.actions)
                {
                    foreach (InputBinding binding in action.bindings)
                    {
                        if (binding.isComposite || string.IsNullOrWhiteSpace(binding.path))
                            continue;

                        string path = binding.path.Trim();
                        if (!owners.TryGetValue(path, out HashSet<string> actions))
                        {
                            actions = new HashSet<string>(StringComparer.Ordinal);
                            owners.Add(path, actions);
                        }
                        actions.Add(action.name);
                    }
                }

                foreach ((string path, HashSet<string> actions) in owners)
                {
                    if (actions.Count <= 1)
                        continue;

                    string key = $"{map.name}|{path}";
                    if (AllowedPhysicalPathSharing.TryGetValue(
                            key,
                            out HashSet<string> allowed)
                        && allowed.SetEquals(actions))
                    {
                        continue;
                    }

                    report.AddError(
                        $"허용되지 않은 물리 경로 중복: {map.name}/{path} = " +
                        string.Join(", ", actions.OrderBy(value => value)));
                }
            }
        }

        private static void ValidateGlyphData(UIInputPromptValidationReport report)
        {
            InputGlyphDataSO data =
                AssetDatabase.LoadAssetAtPath<InputGlyphDataSO>(
                    UIInputPromptBarBuilderUtility.GlyphDataPath);
            if (data == null)
            {
                report.AddError(
                    $"글리프 데이터 누락: {UIInputPromptBarBuilderUtility.GlyphDataPath}");
                return;
            }

            string[] keyboardPaths = { "escape", "enter", "space" };
            string[] gamepadPaths =
            {
                "leftTrigger",
                "rightTrigger",
                "leftShoulder",
                "rightShoulder",
                "buttonSouth",
                "buttonEast",
            };

            foreach (string path in keyboardPaths)
            {
                if (!data.TryResolve(
                        ActiveInputDevice.KeyboardMouse,
                        GamepadBrand.Generic,
                        path,
                        out Sprite sprite)
                    || sprite == null)
                {
                    report.AddError($"키보드 글리프 누락: {path}");
                }
            }

            foreach (string path in gamepadPaths)
            {
                foreach (GamepadBrand brand in new[]
                         {
                             GamepadBrand.Generic,
                             GamepadBrand.Xbox,
                             GamepadBrand.PlayStation,
                             GamepadBrand.Switch,
                         })
                {
                    if (!data.TryResolve(
                            ActiveInputDevice.Gamepad,
                            brand,
                            path,
                            out Sprite sprite)
                        || sprite == null)
                    {
                        report.AddError($"게임패드 글리프 누락: {brand}/{path}");
                    }
                }
            }

            var serialized = new SerializedObject(data);
            string[] facePaths =
            {
                "buttonSouth",
                "buttonEast",
                "buttonWest",
                "buttonNorth",
            };
            ValidateDirectBrandGlyphs(
                serialized.FindProperty("_xboxGlyphs"),
                "Xbox",
                facePaths,
                report);
            ValidateDirectBrandGlyphs(
                serialized.FindProperty("_playStationGlyphs"),
                "PlayStation",
                facePaths,
                report);
            ValidateDirectBrandGlyphs(
                serialized.FindProperty("_switchGlyphs"),
                "Switch",
                facePaths,
                report);
        }

        private static void ValidateDirectBrandGlyphs(
            SerializedProperty entries,
            string brand,
            IReadOnlyList<string> requiredPaths,
            UIInputPromptValidationReport report)
        {
            var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                string path = entry.FindPropertyRelative("controlPath").stringValue;
                UnityEngine.Object sprite =
                    entry.FindPropertyRelative("sprite").objectReferenceValue;
                if (!string.IsNullOrWhiteSpace(path) && sprite != null)
                    mapped.Add(path);
            }

            foreach (string path in requiredPaths)
            {
                if (!mapped.Contains(path))
                    report.AddError($"브랜드 전용 얼굴 버튼 글리프 누락: {brand}/{path}");
            }
        }

        private static void ValidatePrefab(
            PrefabPromptDefinition definition,
            UIInputPromptValidationReport report)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(definition.PrefabPath) == null)
            {
                report.AddError($"프리팹 누락: {definition.PrefabPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(definition.PrefabPath);
            try
            {
                report.CheckedPrefabCount++;
                Transform parent = string.IsNullOrWhiteSpace(definition.ParentPath)
                    ? root.transform
                    : root.transform.Find(definition.ParentPath);
                if (parent == null)
                {
                    report.AddError(
                        $"프롬프트 부모 누락: {definition.PrefabPath} :: {definition.ParentPath}");
                    return;
                }

                Transform barTransform = parent.Find(definition.BarName);
                UIInputPromptBar bar = barTransform != null
                    ? barTransform.GetComponent<UIInputPromptBar>()
                    : null;
                if (bar == null)
                {
                    report.AddError(
                        $"프롬프트 바 누락: {definition.PrefabPath} :: {definition.BarName}");
                    return;
                }

                SerializedObject serialized = new(bar);
                if (serialized.FindProperty("_glyphData").objectReferenceValue == null)
                    report.AddError($"글리프 데이터 참조 누락: {definition.PrefabPath}");

                if (string.Equals(
                        definition.PrefabPath,
                        InventoryPrefabPath,
                        StringComparison.Ordinal))
                {
                    UI_Scene_Inventory inventory = root.GetComponent<UI_Scene_Inventory>();
                    var inventorySerialized = inventory != null
                        ? new SerializedObject(inventory)
                        : null;
                    if (inventorySerialized?
                            .FindProperty("_itemScrollRect")
                            .objectReferenceValue == null)
                    {
                        report.AddError(
                            $"인벤토리 ScrollRect 참조 누락: {definition.PrefabPath}");
                    }
                }

                SerializedProperty entries = serialized.FindProperty("_entries");
                if (entries.arraySize != definition.Prompts.Length)
                {
                    report.AddError(
                        $"프롬프트 수 불일치: {definition.PrefabPath} " +
                        $"{entries.arraySize}/{definition.Prompts.Length}");
                }
                else
                {
                    for (int i = 0; i < entries.arraySize; i++)
                    {
                        SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                        string map = entry.FindPropertyRelative("mapName").stringValue;
                        string action = entry.FindPropertyRelative("actionName").stringValue;
                        if (!string.Equals(
                                map,
                                definition.Prompts[i].MapName,
                                StringComparison.Ordinal)
                            || !string.Equals(
                                action,
                                definition.Prompts[i].ActionName,
                                StringComparison.Ordinal))
                        {
                            report.AddError(
                                $"프롬프트 액션 불일치: {definition.PrefabPath} #{i}");
                        }
                    }
                }

                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.text.Contains("LT / RT", StringComparison.Ordinal)
                        || text.text.Contains("LB / RB", StringComparison.Ordinal)
                        || text.text.Contains("ESC  ", StringComparison.Ordinal)
                        || string.Equals(text.text, "Esc", StringComparison.Ordinal))
                    {
                        report.AddError(
                            $"하드코딩 장치 안내 잔존: {definition.PrefabPath} :: {text.text}");
                    }
                }

                int missingScripts =
                    GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
                if (missingScripts > 0)
                {
                    report.AddError(
                        $"Missing Script {missingScripts}개: {definition.PrefabPath}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    public sealed class UIInputPromptValidationReport
    {
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;
        public bool IsValid => _errors.Count == 0;
        public int CheckedPrefabCount { get; internal set; }

        internal void AddError(string message) => _errors.Add(message);
        internal void AddWarning(string message) => _warnings.Add(message);

        public override string ToString()
        {
            var lines = new List<string>
            {
                $"valid={IsValid}",
                $"checkedPrefabs={CheckedPrefabCount}",
                $"errors={_errors.Count}",
                $"warnings={_warnings.Count}",
            };
            lines.AddRange(_errors.Select(error => $"ERROR: {error}"));
            lines.AddRange(_warnings.Select(warning => $"WARNING: {warning}"));
            return string.Join(Environment.NewLine, lines);
        }
    }
}
