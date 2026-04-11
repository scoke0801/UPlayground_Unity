using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Path;
using UPlayGround.Data.Crafting;
using UPlayGround.Data.Quest;
using UPlayGround.Tool.Editor;

namespace UPlayGround.Editor
{
    /// <summary>
    /// FX / UI / CameraShake / Item / Recipe / Actor 6개 데이터베이스의
    /// ID enum 파일을 한 곳에서 생성하는 통합 창.
    /// 메뉴: UPlayGround/ID Enum Generator
    /// </summary>
    public class IdEnumGeneratorWindow : EditorWindow
    {
        // ── 데이터베이스 설정 ─────────────────────────────────────────

        private class DbConfig
        {
            public string        label;
            public string        enumName;
            public string        outputPath;
            // BuildConfigs 시점에 한 번 계산되어 캐싱됨 (OnGUI에서 AssetDatabase 호출 방지)
            public bool          isFound;
            public int           cachedCount;
            // 실제 생성 수행 (버튼 클릭 시에만 호출)
            public System.Func<bool> Generate;
        }

        private List<DbConfig> _configs;

        // ── 스타일 ────────────────────────────────────────────────────
        private static readonly Color ColorHeader  = new(0.15f, 0.15f, 0.20f);
        private static readonly Color ColorRowAlt  = new(0.20f, 0.20f, 0.22f);
        private static readonly Color ColorOk      = new(0.35f, 0.80f, 0.45f);
        private static readonly Color ColorMissing = new(0.85f, 0.45f, 0.25f);
        private GUIStyle _labelBold;
        private bool     _stylesReady;

        // ── 메뉴 ─────────────────────────────────────────────────────
        [MenuItem("UPlayGround/ID Enum Generator")]
        public static void Open()
        {
            var win = GetWindow<IdEnumGeneratorWindow>("ID Enum Generator");
            win.minSize = new Vector2(680f, 320f);
            win.Show();
        }

        // ── 라이프사이클 ─────────────────────────────────────────────
        private void OnEnable() => BuildConfigs();
        private void OnFocus()  => BuildConfigs(); // DB 추가/삭제 반영 (포커스 시 1회만 탐색)

        /// <summary>
        /// 각 DB를 AssetDatabase에서 찾아 캐싱한다.
        /// OnGUI에서는 캐싱된 값만 읽어 per-frame AssetDatabase 탐색을 방지한다.
        /// </summary>
        private void BuildConfigs()
        {
            _configs = new List<DbConfig>
            {
                BuildFxConfig(),
                BuildUiConfig(),
                BuildCameraShakeConfig(),
                BuildItemConfig(),
                BuildRecipeConfig(),
                BuildActorConfig(),
                BuildQuestConfig(),
            };
        }

        private void OnGUI()
        {
            InitStyles();

            // ── 툴바 ─────────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("ID Enum Generator", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(66)))
                BuildConfigs();
            if (GUILayout.Button("전체 생성", EditorStyles.toolbarButton, GUILayout.Width(76)))
                GenerateAll();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // ── 헤더 행 ──────────────────────────────────────────────
            DrawColorBox(ColorHeader, 22);
            var hdrRect = GUILayoutUtility.GetLastRect();
            GUI.Label(new Rect(hdrRect.x +   8, hdrRect.y + 3,  160, 16), "데이터베이스",  EditorStyles.boldLabel);
            GUI.Label(new Rect(hdrRect.x + 168, hdrRect.y + 3,   70, 16), "상태",          EditorStyles.boldLabel);
            GUI.Label(new Rect(hdrRect.x + 238, hdrRect.y + 3,   60, 16), "항목 수",       EditorStyles.boldLabel);
            GUI.Label(new Rect(hdrRect.x + 298, hdrRect.y + 3,  300, 16), "출력 경로",     EditorStyles.boldLabel);

            // ── 항목 행 (캐싱된 값만 읽음 — AssetDatabase 호출 없음) ──
            for (int i = 0; i < _configs.Count; i++)
            {
                var cfg  = _configs[i];
                bool alt = i % 2 == 1;

                Rect rowRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
                if (alt) EditorGUI.DrawRect(rowRect, ColorRowAlt);

                float x = rowRect.x + 8;
                float y = rowRect.y + 5;

                // 레이블
                GUI.Label(new Rect(x, y, 158, 18), cfg.label, _labelBold);

                // 상태 배지
                var prevColor = GUI.contentColor;
                GUI.contentColor = cfg.isFound ? ColorOk : ColorMissing;
                GUI.Label(new Rect(x + 160, y, 68, 18), cfg.isFound ? "✓ 있음" : "✗ 없음", EditorStyles.boldLabel);
                GUI.contentColor = prevColor;

                // 항목 수
                GUI.Label(new Rect(x + 230, y, 58, 18),
                    cfg.cachedCount >= 0 ? $"{cfg.cachedCount}개" : "-", EditorStyles.label);

                // 출력 경로
                GUI.Label(new Rect(x + 290, y, 310, 18), cfg.outputPath, EditorStyles.miniLabel);

                // 생성 버튼
                using (new EditorGUI.DisabledScope(!cfg.isFound))
                {
                    if (GUI.Button(new Rect(rowRect.xMax - 56, rowRect.y + 4, 50, 20), "생성"))
                    {
                        if (cfg.Generate())
                        {
                            AssetDatabase.Refresh();
                            EditorUtility.DisplayDialog("생성 완료",
                                $"{cfg.enumName} 생성 완료\n→ {cfg.outputPath}", "확인");
                        }
                    }
                }
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "int ID 기반(Item, Recipe): enum 값 자체가 ID입니다. ToXxx()는 (int)type을 반환합니다.\n" +
                "string Key 기반(FX, UI, CameraShake, Actor): ToKey() / ToActorId()로 원본 문자열을 반환합니다.\n" +
                "DB 추가/변경 후에는 [새로고침]을 눌러 상태를 갱신하세요.",
                MessageType.Info);
        }

        // ── 전체 생성 ─────────────────────────────────────────────────
        private void GenerateAll()
        {
            int success = 0, skip = 0;
            foreach (var cfg in _configs)
            {
                if (!cfg.isFound) { skip++; continue; }
                if (cfg.Generate()) success++;
                else skip++;
            }
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("전체 생성 완료",
                $"성공: {success}개\n건너뜀(DB 없음 또는 오류): {skip}개", "확인");
        }

        // ── 개별 DbConfig 빌더 ─────────────────────────────────────────
        // 각 빌더는 FindDb를 여기서 1회 호출해 db 참조를 람다에 캡처한다.
        // OnGUI/GenerateAll에서는 캐싱된 isFound·cachedCount만 읽는다.

        private static DbConfig BuildFxConfig()
        {
            const string outputPath = "Assets/02.Scripts/Data/Path/FXKeyType.cs";
            var db      = FindDb<FXPrefabDatabase>();
            var raw     = db != null ? GetStringEntriesFromPrefabList(db) : null;
            int count   = raw?.Count ?? -1;
            return new DbConfig
            {
                label       = "FX Prefab",
                enumName    = "FXKeyType",
                outputPath  = outputPath,
                isFound     = db != null,
                cachedCount = count,
                Generate = () =>
                {
                    if (db == null) return false;
                    var entries = IdEnumGeneratorUtility.DeduplicateEntries(raw);
                    return IdEnumGeneratorUtility.GenerateStringKeyEnum(
                        "FXKeyType", "ToKey", "FX Prefab",
                        outputPath, "UPlayGround.Data.Path", entries);
                },
            };
        }

        private static DbConfig BuildUiConfig()
        {
            const string outputPath = "Assets/02.Scripts/Data/Path/UIKeyType.cs";
            var db      = FindDb<UIPrefabDatabase>();
            var raw     = db != null ? GetStringEntriesFromPrefabList(db) : null;
            int count   = raw?.Count ?? -1;
            return new DbConfig
            {
                label       = "UI Prefab",
                enumName    = "UIKeyType",
                outputPath  = outputPath,
                isFound     = db != null,
                cachedCount = count,
                Generate = () =>
                {
                    if (db == null) return false;
                    var entries = IdEnumGeneratorUtility.DeduplicateEntries(raw);
                    return IdEnumGeneratorUtility.GenerateStringKeyEnum(
                        "UIKeyType", "ToKey", "UI Prefab",
                        outputPath, "UPlayGround.Data.Path", entries);
                },
            };
        }

        private static DbConfig BuildCameraShakeConfig()
        {
            const string outputPath = "Assets/02.Scripts/Data/Path/CameraShakeIdType.cs";
            var db = FindDb<CameraShakeDatabase>();
            var raw = new List<(string, string)>();
            if (db != null)
                foreach (var item in db.AllItems)
                    if (item != null && !string.IsNullOrEmpty(item.key))
                        raw.Add((item.key, item.key));
            return new DbConfig
            {
                label       = "Camera Shake",
                enumName    = "CameraShakeIdType",
                outputPath  = outputPath,
                isFound     = db != null,
                cachedCount = db != null ? raw.Count : -1,
                Generate = () =>
                {
                    if (db == null) return false;
                    var entries = IdEnumGeneratorUtility.DeduplicateEntries(raw);
                    return IdEnumGeneratorUtility.GenerateStringKeyEnum(
                        "CameraShakeIdType", "ToKey", "CameraShake",
                        outputPath, "UPlayGround.Data.Path", entries);
                },
            };
        }

        private static DbConfig BuildItemConfig()
        {
            const string outputPath = "Assets/02.Scripts/Data/Item/ItemIdType.cs";
            var db  = FindDb<ItemDatabase>();
            var raw = new List<(string, int)>();
            if (db != null)
                foreach (var item in db.AllItems)
                {
                    if (item == null) continue;
                    string name = string.IsNullOrEmpty(item.itemName)
                        ? $"Item_{item.itemId}" : item.itemName;
                    raw.Add((name, item.itemId));
                }
            return new DbConfig
            {
                label       = "Item",
                enumName    = "ItemIdType",
                outputPath  = outputPath,
                isFound     = db != null,
                cachedCount = db != null ? raw.Count : -1,
                Generate = () =>
                {
                    if (db == null) return false;
                    var entries = IdEnumGeneratorUtility.DeduplicateEntries(raw);
                    return IdEnumGeneratorUtility.GenerateIntKeyEnum(
                        "ItemIdType", "ToItemId", "Item",
                        outputPath, "UPlayGround.Data.Item", entries);
                },
            };
        }

        private static DbConfig BuildRecipeConfig()
        {
            const string outputPath = "Assets/02.Scripts/Data/Crafting/RecipeIdType.cs";
            var db  = FindDb<RecipeDatabase>();
            var raw = new List<(string, int)>();
            if (db != null)
                foreach (var recipe in db.AllRecipes)
                {
                    string name = string.IsNullOrEmpty(recipe.recipeName)
                        ? $"Recipe_{recipe.recipeID}" : recipe.recipeName;
                    raw.Add((name, recipe.recipeID));
                }
            return new DbConfig
            {
                label       = "Recipe",
                enumName    = "RecipeIdType",
                outputPath  = outputPath,
                isFound     = db != null,
                cachedCount = db != null ? raw.Count : -1,
                Generate = () =>
                {
                    if (db == null) return false;
                    var entries = IdEnumGeneratorUtility.DeduplicateEntries(raw);
                    return IdEnumGeneratorUtility.GenerateIntKeyEnum(
                        "RecipeIdType", "ToRecipeId", "Recipe",
                        outputPath, "UPlayGround.Data.Crafting", entries);
                },
            };
        }

        private static DbConfig BuildActorConfig()
        {
            const string outputPath = "Assets/02.Scripts/Data/Actor/ActorIdType.cs";
            var db  = FindDb<ActorDatabase>();
            var raw = new List<(string, string)>();
            if (db != null)
                foreach (var def in db.All)
                    if (def != null && !string.IsNullOrEmpty(def.actorId))
                        raw.Add((def.actorId, def.actorId));
            return new DbConfig
            {
                label       = "Actor",
                enumName    = "ActorIdType",
                outputPath  = outputPath,
                isFound     = db != null,
                cachedCount = db != null ? raw.Count : -1,
                Generate = () =>
                {
                    if (db == null) return false;
                    var entries = IdEnumGeneratorUtility.DeduplicateEntries(raw);
                    return IdEnumGeneratorUtility.GenerateStringKeyEnum(
                        "ActorIdType", "ToActorId", "Actor",
                        outputPath, "UPlayGround.Data.Actor", entries);
                },
            };
        }

        private static DbConfig BuildQuestConfig()
        {
            const string outputPath = "Assets/02.Scripts/Data/Quest/QuestIdType.cs";
            var db  = FindDb<QuestDatabase>();
            var raw = new List<(string, string)>();
            if (db != null)
                foreach (var quest in db.QuestList)
                {
                    if (quest == null || string.IsNullOrEmpty(quest.questId)) continue;
                    raw.Add((quest.questId, quest.questId));
                }
            return new DbConfig
            {
                label       = "Quest",
                enumName    = "QuestIdType",
                outputPath  = outputPath,
                isFound     = db != null,
                cachedCount = db != null ? raw.Count : -1,
                Generate = () =>
                {
                    if (db == null) return false;
                    var entries = IdEnumGeneratorUtility.DeduplicateEntries(raw);
                    return IdEnumGeneratorUtility.GenerateStringKeyEnum(
                        "QuestIdType", "ToQuestId", "Quest ID",
                        outputPath, "UPlayGround.Data.Quest", entries);
                },
            };
        }

        // ── 공통 헬퍼 ─────────────────────────────────────────────────

        private static T FindDb<T>() where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        /// <summary>
        /// SerializedObject를 통해 "prefabs" 배열의 "key" 필드 목록을 읽는다.
        /// FXPrefabDatabase / UIPrefabDatabase 공용.
        /// </summary>
        private static List<(string rawName, string key)> GetStringEntriesFromPrefabList(ScriptableObject db)
        {
            var result = new List<(string, string)>();
            var so     = new SerializedObject(db);
            var arr    = so.FindProperty("prefabs");
            if (arr == null) return result;
            for (int i = 0; i < arr.arraySize; i++)
            {
                string key = arr.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue;
                if (!string.IsNullOrEmpty(key)) result.Add((key, key));
            }
            return result;
        }

        // ── 스타일 초기화 ─────────────────────────────────────────────
        private void InitStyles()
        {
            if (_stylesReady) return;
            _labelBold = new GUIStyle(EditorStyles.boldLabel)
                { alignment = TextAnchor.MiddleLeft };
            _stylesReady = true;
        }

        private void DrawColorBox(Color color, float height)
        {
            var rect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, color);
        }
    }
}
