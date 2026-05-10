#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UPlayGround.Editor
{
    public class UPlaygroundToolsLauncher : EditorWindow
    {
        private readonly struct ToolEntry
        {
            public readonly string Name;
            public readonly string MenuPath;

            public ToolEntry(string name, string menuPath)
            {
                Name     = name;
                MenuPath = menuPath;
            }
        }

        private static readonly (string Category, ToolEntry[] Tools)[] s_categories =
        {
            ("Generator Tool", new[]
            {
                new ToolEntry("ID Enum Generator",               "UPlayGround/Generator Tool/ID Enum Generator"),
                new ToolEntry("Item Data Generator",             "UPlayGround/Generator Tool/Item Data Generator"),
                new ToolEntry("Recipe Data Generator",           "UPlayGround/Generator Tool/Recipe Data Generator"),
                new ToolEntry("Stat Data Generator",             "UPlayGround/Generator Tool/Stat Data Generator"),
                new ToolEntry("Validate Stat Coverage",          "UPlayGround/Generator Tool/Validate Stat Data Coverage"),
                new ToolEntry("Party Growth Editor",             "UPlayGround/Generator Tool/Party Growth Editor"),
                new ToolEntry("NPC Data Generator",              "UPlayGround/Generator Tool/NPC Data Generator"),
                new ToolEntry("Main Story Generator",            "UPlayGround/Generator Tool/Main Story Generator"),
                new ToolEntry("Sub Story Generator",             "UPlayGround/Generator Tool/Sub Story Generator"),
                new ToolEntry("Locomotion Motion Setup",         "UPlayGround/Generator Tool/Locomotion Motion Setup"),
                new ToolEntry("Camera Shake Presets",            "UPlayGround/Generator Tool/Camera Shake Presets"),
            }),
            ("Character / Actor", new[]
            {
                new ToolEntry("Actor Database Editor",           "UPlayGround/Character/Actor/Actor Database Editor"),
                new ToolEntry("Actor Runtime Monitor",           "UPlayGround/Character/Actor/Actor Runtime Monitor"),
                new ToolEntry("Lossy Scale Inspector",           "UPlayGround/Character/Actor/Lossy Scale Inspector"),
                new ToolEntry("애니메이션 에디터",                 "UPlayGround/Character/Actor/애니메이션 에디터"),
                new ToolEntry("Export Monster Data",             "UPlayGround/Character/Actor/Data/Export Monster Data"),
                new ToolEntry("Import Monster Data",             "UPlayGround/Character/Actor/Data/Import Monster Data"),
            }),
            ("Character / AI", new[]
            {
                new ToolEntry("Behavior Tree Editor",            "UPlayGround/Character/AI/Behavior Tree Editor"),
                new ToolEntry("Generate BT Ground Test",         "UPlayGround/Character/AI/Behavior Tree/Generate Enemy Ground Basic Test"),
                new ToolEntry("BT Json Export",                  "UPlayGround/Character/AI/Behavior Tree Json/Export Selected"),
                new ToolEntry("BT Json Import",                  "UPlayGround/Character/AI/Behavior Tree Json/Import Json"),
            }),
            ("Gameplay / Combat", new[]
            {
                new ToolEntry("공격 데이터 에디터",                "UPlayGround/Gameplay/Combat/공격 데이터 에디터"),
                new ToolEntry("MotionSet 기반 공격 데이터 생성기", "UPlayGround/Gameplay/Combat/MotionSet 기반 공격 데이터 생성기"),
            }),
            ("Gameplay / Item", new[]
            {
                new ToolEntry("Item Editor",                     "UPlayGround/Gameplay/Item/Item Editor"),
                new ToolEntry("Drop Table Editor",               "UPlayGround/Gameplay/Item/Drop Table Editor"),
                new ToolEntry("Weapon Definition (Add Missing)", "UPlayGround/Gameplay/Item/WeaponDefinition/Create Missing Definitions"),
                new ToolEntry("Weapon Definition (Regen All)",   "UPlayGround/Gameplay/Item/WeaponDefinition/Regenerate All Definitions"),
            }),
            ("Gameplay / Crafting", new[]
            {
                new ToolEntry("Recipe Editor",                   "UPlayGround/Gameplay/Crafting/Recipe Editor"),
                new ToolEntry("Import Recipe Data",              "UPlayGround/Gameplay/Crafting/Import Recipe Data"),
            }),
            ("Gameplay / Stat", new[]
            {
                new ToolEntry("Stat Database Editor",            "UPlayGround/Gameplay/Stat/Stat Database Editor"),
                new ToolEntry("Stat Runtime Monitor",            "UPlayGround/Gameplay/Stat/Stat Runtime Monitor"),
            }),
            ("Gameplay / Quest", new[]
            {
                new ToolEntry("Quest Editor",                    "UPlayGround/Gameplay/Quest/Quest Editor"),
            }),
            ("Gameplay / GameplayTag", new[]
            {
                new ToolEntry("Tag Registry Editor",             "UPlayGround/Gameplay/GameplayTag/Tag Registry Editor"),
            }),
            ("World / Map", new[]
            {
                new ToolEntry("Map Placement Tool",              "UPlayGround/World/Map/Map Placement Tool"),
            }),
            ("World / Minimap", new[]
            {
                new ToolEntry("Minimap Capture Editor",          "UPlayGround/World/Minimap/Minimap Capture Editor"),
            }),
            ("World / Camera", new[]
            {
                new ToolEntry("Create Dialogue Camera Settings", "UPlayGround/World/Camera/Create Dialogue Camera Settings"),
            }),
            ("Narrative / Dialogue", new[]
            {
                new ToolEntry("Speaker Actor Binding",           "UPlayGround/Narrative/Dialogue/Speaker Actor Binding Generator"),
            }),
            ("Narrative / Story", new[]
            {
                new ToolEntry("Dialogue Graph Editor",           "UPlayGround/Narrative/Story/Dialogue Graph Editor"),
            }),
            ("Util", new[]
            {
                new ToolEntry("치트 콘솔",                        "UPlayGround/Util/치트 콘솔"),
                new ToolEntry("Avatar Armature Bake",            "UPlayGround/Util/Avatar Armature Bake Tool"),
                new ToolEntry("Weapon Motion Setup",             "UPlayGround/Util/Weapon Motion Setup"),
                new ToolEntry("Animation Binding Remap",         "UPlayGround/Util/Animation Binding Remap Test"),
                new ToolEntry("Actor Screenshot Tool",           "UPlayGround/Util/Actor Screenshot Tool"),
                new ToolEntry("Background Color Remover",        "UPlayGround/Util/Background Color Remover"),
                new ToolEntry("URP 머티리얼 변환기",               "UPlayGround/Util/Converter/URP 머티리얼 변환기"),
                new ToolEntry("JSON Table Viewer",               "UPlayGround/Util/Viewer/JSON Table Viewer"),
                new ToolEntry("Missing Script 제거",             "UPlayGround/Util/Missing Script 제거/선택 오브젝트 하위 전체"),
            }),
        };

        private SearchField _searchField;
        private string _searchQuery = "";
        private Vector2 _scroll;
        private readonly Dictionary<string, bool> _foldouts = new();

        [MenuItem("UPlayGround/Tools Launcher", priority = 1)]
        public static void Open()
        {
            var win = GetWindow<UPlaygroundToolsLauncher>("Tools Launcher");
            win.minSize = new Vector2(280f, 400f);
            win.Show();
        }

        private void OnEnable()
        {
            _searchField = new SearchField();
            foreach (var (cat, _) in s_categories)
                if (!_foldouts.ContainsKey(cat)) _foldouts[cat] = true;
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSearchBar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawCategories();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("UPlayGround Tools", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("모두 열기",  EditorStyles.toolbarButton, GUILayout.MinWidth(60)))
                SetAllFoldouts(true);
            if (GUILayout.Button("모두 닫기", EditorStyles.toolbarButton, GUILayout.MinWidth(60)))
                SetAllFoldouts(false);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.Space(3);
            var rect = EditorGUILayout.GetControlRect(false, 20f);
            rect.x     += 4;
            rect.width -= 8;
            _searchQuery = _searchField.OnGUI(rect, _searchQuery);
            EditorGUILayout.Space(3);
        }

        private void DrawCategories()
        {
            bool filtering = !string.IsNullOrWhiteSpace(_searchQuery);
            string q = _searchQuery.ToLower();

            foreach (var (category, tools) in s_categories)
            {
                ToolEntry[] matches = filtering
                    ? System.Array.FindAll(tools, t => t.Name.ToLower().Contains(q))
                    : tools;

                if (matches.Length == 0) continue;

                if (filtering)
                {
                    EditorGUILayout.LabelField(category, EditorStyles.boldLabel);
                }
                else
                {
                    if (!_foldouts.TryGetValue(category, out bool open)) open = true;
                    _foldouts[category] = EditorGUILayout.Foldout(open, category, true, EditorStyles.foldoutHeader);
                    if (!_foldouts[category]) continue;
                }

                EditorGUI.indentLevel++;
                foreach (var tool in matches)
                {
                    if (GUILayout.Button(tool.Name, GUILayout.Height(22)))
                        EditorApplication.ExecuteMenuItem(tool.MenuPath);
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(2);
            }
        }

        private void SetAllFoldouts(bool value)
        {
            foreach (var (cat, _) in s_categories)
                _foldouts[cat] = value;
        }
    }
}
#endif
