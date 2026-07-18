namespace UPlayGround.Data.Path
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(UIPrefabDatabase))]
    public class UIPrefabDatabaseEditor : UnityEditor.Editor
    {
        private const float PreviewSize = 42f;

        private SerializedProperty _prefabsProp;
        private readonly Dictionary<string, int> _keyCounts = new Dictionary<string, int>();
        private readonly HashSet<string> _uiKeyTypeKeys = new HashSet<string>();

        private string _searchText = "";
        private Vector2 _scroll;
        private bool _showOnlyIssues;
        private bool _showEnumSync = true;

        private void OnEnable()
        {
            _prefabsProp = serializedObject.FindProperty("prefabs");
            RebuildValidationCache();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            RebuildValidationCache();

            DrawHeader_Custom();
            DrawToolbar();
            DrawValidationSummary();

            EditorGUILayout.Space(4);
            DrawEntryList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader_Custom()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("UI Prefab Database", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"등록 {_prefabsProp.arraySize}개 / 검색 {GetVisibleIndexes().Count}개",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("새 항목", GUILayout.Width(72), GUILayout.Height(24)))
                AddEntry();

            if (GUILayout.Button("키 정렬", GUILayout.Width(72), GUILayout.Height(24)))
                SortByKey();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("검색", GUILayout.Width(30));
            string newSearch = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
            if (newSearch != _searchText)
                _searchText = newSearch;

            if (GUILayout.Button("지우기", EditorStyles.toolbarButton, GUILayout.Width(48)))
            {
                _searchText = "";
                GUI.FocusControl(null);
            }

            GUILayout.Space(8);
            _showOnlyIssues = GUILayout.Toggle(_showOnlyIssues, "문제만", EditorStyles.toolbarButton, GUILayout.Width(58));
            _showEnumSync = GUILayout.Toggle(_showEnumSync, "Enum 비교", EditorStyles.toolbarButton, GUILayout.Width(72));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawValidationSummary()
        {
            int emptyKeyCount = 0;
            int missingPrefabCount = 0;
            int duplicateKeyCount = _keyCounts.Count(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value > 1);

            for (int i = 0; i < _prefabsProp.arraySize; i++)
            {
                SerializedProperty entry = _prefabsProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrWhiteSpace(GetString(entry, "key")))
                    emptyKeyCount++;
                if (entry.FindPropertyRelative("prefab").objectReferenceValue == null)
                    missingPrefabCount++;
            }

            MessageType type = duplicateKeyCount > 0 || emptyKeyCount > 0 || missingPrefabCount > 0
                ? MessageType.Warning
                : MessageType.Info;

            EditorGUILayout.HelpBox(
                $"중복 키 {duplicateKeyCount}개, 빈 키 {emptyKeyCount}개, 프리팹 없음 {missingPrefabCount}개",
                type);

            if (_showEnumSync)
                DrawEnumSyncSummary();
        }

        private void DrawEnumSyncSummary()
        {
            var dbKeys = GetDatabaseKeys();
            var missingInDb = _uiKeyTypeKeys.Where(key => !dbKeys.Contains(key)).OrderBy(key => key).ToList();
            var missingInEnum = dbKeys.Where(key => !_uiKeyTypeKeys.Contains(key)).OrderBy(key => key).ToList();

            if (missingInDb.Count == 0 && missingInEnum.Count == 0)
            {
                EditorGUILayout.HelpBox("UIKeyType과 DB 키가 일치합니다.", MessageType.Info);
                return;
            }

            if (missingInDb.Count > 0)
                EditorGUILayout.HelpBox($"UIKeyType에는 있지만 DB에 없는 키: {string.Join(", ", missingInDb)}", MessageType.Warning);

            if (missingInEnum.Count > 0)
                EditorGUILayout.HelpBox($"DB에는 있지만 UIKeyType에 없는 키: {string.Join(", ", missingInEnum)}", MessageType.Warning);
        }

        private void DrawEntryList()
        {
            List<int> visibleIndexes = GetVisibleIndexes();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (visibleIndexes.Count == 0)
            {
                EditorGUILayout.Space(20);
                GUILayout.Label("검색 결과가 없습니다.", EditorStyles.centeredGreyMiniLabel);
            }

            foreach (int index in visibleIndexes)
                DrawEntry(index);

            EditorGUILayout.EndScrollView();
        }

        private void DrawEntry(int index)
        {
            SerializedProperty entry = _prefabsProp.GetArrayElementAtIndex(index);
            SerializedProperty keyProp = entry.FindPropertyRelative("key");
            SerializedProperty prefabProp = entry.FindPropertyRelative("prefab");
            SerializedProperty layerProp = entry.FindPropertyRelative("defaultLayer");
            SerializedProperty descProp = entry.FindPropertyRelative("description");

            bool hasIssue = HasIssue(entry);
            Color oldColor = GUI.backgroundColor;
            if (hasIssue)
                GUI.backgroundColor = new Color(1f, 0.82f, 0.55f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = oldColor;

            EditorGUILayout.BeginHorizontal();
            Texture preview = AssetPreview.GetAssetPreview(prefabProp.objectReferenceValue);
            Rect previewRect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.Width(PreviewSize), GUILayout.Height(PreviewSize));
            if (preview != null)
                GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
            else
                GUI.Label(previewRect, "No\nPrefab", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(keyProp, GUIContent.none);

            GUI.enabled = index > 0;
            if (GUILayout.Button("위", GUILayout.Width(32)))
                MoveEntry(index, index - 1);
            GUI.enabled = index < _prefabsProp.arraySize - 1;
            if (GUILayout.Button("아래", GUILayout.Width(42)))
                MoveEntry(index, index + 1);
            GUI.enabled = true;

            if (GUILayout.Button("삭제", GUILayout.Width(42)))
            {
                DeleteEntry(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prefabProp, GUIContent.none);
            if (prefabProp.objectReferenceValue != null && GUILayout.Button("Ping", GUILayout.Width(42)))
                EditorGUIUtility.PingObject(prefabProp.objectReferenceValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(layerProp, new GUIContent("기본 레이어"));
            EditorGUILayout.PropertyField(descProp, new GUIContent("설명"));
            DrawEntryIssues(entry);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawEntryIssues(SerializedProperty entry)
        {
            string key = GetString(entry, "key");
            bool hasDuplicate = !string.IsNullOrWhiteSpace(key) && _keyCounts.TryGetValue(key, out int count) && count > 1;

            if (string.IsNullOrWhiteSpace(key))
                EditorGUILayout.HelpBox("키가 비어 있습니다.", MessageType.Error);
            else if (hasDuplicate)
                EditorGUILayout.HelpBox($"중복된 키입니다: {key}", MessageType.Error);

            if (entry.FindPropertyRelative("prefab").objectReferenceValue == null)
                EditorGUILayout.HelpBox("프리팹이 지정되지 않았습니다.", MessageType.Warning);

            if (!string.IsNullOrWhiteSpace(key) && !_uiKeyTypeKeys.Contains(key))
                EditorGUILayout.HelpBox("UIKeyType에 아직 없는 키입니다. 필요하면 ID Enum Generator로 재생성하세요.", MessageType.Info);
        }

        private void AddEntry()
        {
            Undo.RecordObject(target, "UI Prefab 항목 추가");
            int index = _prefabsProp.arraySize;
            _prefabsProp.InsertArrayElementAtIndex(index);

            SerializedProperty entry = _prefabsProp.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("key").stringValue = GetUniqueNewKey();
            entry.FindPropertyRelative("prefab").objectReferenceValue = null;
            entry.FindPropertyRelative("defaultLayer").enumValueIndex = 2;
            entry.FindPropertyRelative("description").stringValue = "";

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private void DeleteEntry(int index)
        {
            if (!EditorUtility.DisplayDialog("UI Prefab 항목 삭제", $"{GetString(_prefabsProp.GetArrayElementAtIndex(index), "key")} 항목을 삭제합니다.", "삭제", "취소"))
                return;

            Undo.RecordObject(target, "UI Prefab 항목 삭제");
            _prefabsProp.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private void MoveEntry(int from, int to)
        {
            Undo.RecordObject(target, "UI Prefab 항목 이동");
            _prefabsProp.MoveArrayElement(from, to);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private void SortByKey()
        {
            Undo.RecordObject(target, "UI Prefab 키 정렬");

            var entries = new List<EntrySnapshot>();
            for (int i = 0; i < _prefabsProp.arraySize; i++)
                entries.Add(EntrySnapshot.From(_prefabsProp.GetArrayElementAtIndex(i)));

            entries = entries.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase).ToList();
            for (int i = 0; i < entries.Count; i++)
                entries[i].ApplyTo(_prefabsProp.GetArrayElementAtIndex(i));

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private List<int> GetVisibleIndexes()
        {
            var result = new List<int>();

            for (int i = 0; i < _prefabsProp.arraySize; i++)
            {
                SerializedProperty entry = _prefabsProp.GetArrayElementAtIndex(i);
                if (_showOnlyIssues && !HasIssue(entry))
                    continue;
                if (!MatchesSearch(entry))
                    continue;
                result.Add(i);
            }

            return result;
        }

        private bool MatchesSearch(SerializedProperty entry)
        {
            if (string.IsNullOrWhiteSpace(_searchText))
                return true;

            string search = _searchText.Trim();
            return Contains(GetString(entry, "key"), search)
                || Contains(GetString(entry, "description"), search)
                || Contains(entry.FindPropertyRelative("defaultLayer").enumDisplayNames[entry.FindPropertyRelative("defaultLayer").enumValueIndex], search)
                || Contains(entry.FindPropertyRelative("prefab").objectReferenceValue != null
                    ? entry.FindPropertyRelative("prefab").objectReferenceValue.name
                    : "", search);
        }

        private bool HasIssue(SerializedProperty entry)
        {
            string key = GetString(entry, "key");
            if (string.IsNullOrWhiteSpace(key))
                return true;
            if (_keyCounts.TryGetValue(key, out int count) && count > 1)
                return true;
            if (entry.FindPropertyRelative("prefab").objectReferenceValue == null)
                return true;
            return false;
        }

        private void RebuildValidationCache()
        {
            _keyCounts.Clear();
            _uiKeyTypeKeys.Clear();

            if (_prefabsProp != null)
            {
                for (int i = 0; i < _prefabsProp.arraySize; i++)
                {
                    string key = GetString(_prefabsProp.GetArrayElementAtIndex(i), "key");
                    if (!_keyCounts.ContainsKey(key))
                        _keyCounts.Add(key, 0);
                    _keyCounts[key]++;
                }
            }

            foreach (UIKeyType value in Enum.GetValues(typeof(UIKeyType)))
            {
                if (value == UIKeyType.None)
                    continue;

                string key = value.ToKey();
                if (!string.IsNullOrWhiteSpace(key))
                    _uiKeyTypeKeys.Add(key);
            }
        }

        private HashSet<string> GetDatabaseKeys()
        {
            var keys = new HashSet<string>();
            for (int i = 0; i < _prefabsProp.arraySize; i++)
            {
                string key = GetString(_prefabsProp.GetArrayElementAtIndex(i), "key");
                if (!string.IsNullOrWhiteSpace(key))
                    keys.Add(key);
            }

            return keys;
        }

        private string GetUniqueNewKey()
        {
            const string baseKey = "NewUI";
            string key = baseKey;
            int index = 1;
            HashSet<string> existing = GetDatabaseKeys();
            while (existing.Contains(key))
            {
                index++;
                key = $"{baseKey}{index}";
            }

            return key;
        }

        private static string GetString(SerializedProperty entry, string propertyName)
        {
            return entry.FindPropertyRelative(propertyName).stringValue ?? "";
        }

        private static bool Contains(string source, string search)
        {
            return source.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private struct EntrySnapshot
        {
            public string Key;
            public UnityEngine.Object Prefab;
            public int LayerIndex;
            public string Description;

            public static EntrySnapshot From(SerializedProperty entry)
            {
                return new EntrySnapshot
                {
                    Key = entry.FindPropertyRelative("key").stringValue,
                    Prefab = entry.FindPropertyRelative("prefab").objectReferenceValue,
                    LayerIndex = entry.FindPropertyRelative("defaultLayer").enumValueIndex,
                    Description = entry.FindPropertyRelative("description").stringValue
                };
            }

            public void ApplyTo(SerializedProperty entry)
            {
                entry.FindPropertyRelative("key").stringValue = Key;
                entry.FindPropertyRelative("prefab").objectReferenceValue = Prefab;
                entry.FindPropertyRelative("defaultLayer").enumValueIndex = LayerIndex;
                entry.FindPropertyRelative("description").stringValue = Description;
            }
        }
    }
#endif
}
