using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Data.Sound.Editor
{
    [CustomEditor(typeof(SoundDatabaseSO))]
    public sealed class SoundDatabaseSOEditor : UnityEditor.Editor
    {
        private const float ListMaxHeight = 430f;
        private const float StatusWidth = 22f;
        private const float BusWidth = 78f;
        private const float ActionWidth = 48f;

        private readonly List<ValidationMessage> _messages = new();
        private readonly List<int> _visibleIndices = new();

        private SerializedProperty _entriesProperty;
        private UnityEditor.Editor _entryEditor;
        private SoundEntrySO _selectedEntry;
        private Vector2 _listScroll;
        private string _searchText = string.Empty;
        private BusFilter _busFilter = BusFilter.전체;
        private bool _issuesOnly;
        private bool _showValidation;
        private bool _showDetails = true;

        private enum BusFilter
        {
            전체 = -1,
            Master = SoundBusType.Master,
            BGM = SoundBusType.BGM,
            SFX = SoundBusType.SFX,
            UI = SoundBusType.UI,
            Voice = SoundBusType.Voice,
            Ambience = SoundBusType.Ambience
        }

        private void OnEnable()
        {
            _entriesProperty = serializedObject.FindProperty("entries");
            RefreshValidation();
        }

        private void OnDisable()
        {
            DestroyEntryEditor();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            RefreshValidation();
            BuildVisibleIndices();

            DrawHeader();
            DrawToolbar();
            DrawBulkActions();
            DrawDropArea();
            DrawTable();

            serializedObject.ApplyModifiedProperties();

            DrawSelectedEntry();
            DrawValidation();
        }

        private new void DrawHeader()
        {
            var database = (SoundDatabaseSO)target;
            CountValidation(out int errorCount, out int warningCount);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Sound Database", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(
                        $"{_entriesProperty.arraySize}개 · 오류 {errorCount} · 경고 {warningCount}",
                        EditorStyles.miniLabel);
                }

                EditorGUILayout.LabelField(
                    "검색과 버스 필터로 항목을 좁히고, 행을 선택해 아래에서 상세 설정을 편집합니다.",
                    EditorStyles.wordWrappedMiniLabel);

                if (database.Entries.Count == 0)
                    EditorGUILayout.HelpBox("등록된 SoundEntry가 없습니다. 프로젝트 동기화 또는 드래그 앤 드롭으로 추가하세요.", MessageType.Info);
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _searchText = GUILayout.TextField(
                    _searchText,
                    GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.toolbarSearchField,
                    GUILayout.MinWidth(120f));

                _busFilter = (BusFilter)EditorGUILayout.EnumPopup(
                    _busFilter,
                    EditorStyles.toolbarPopup,
                    GUILayout.Width(92f));

                _issuesOnly = GUILayout.Toggle(
                    _issuesOnly,
                    new GUIContent("문제만", "오류 또는 경고가 있는 항목만 표시합니다."),
                    EditorStyles.toolbarButton,
                    GUILayout.Width(58f));

                GUILayout.FlexibleSpace();
                GUILayout.Label($"{_visibleIndices.Count}/{_entriesProperty.arraySize}", EditorStyles.miniLabel);
            }
        }

        private void DrawBulkActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("선택 항목 추가", "Project 창에서 선택한 SoundEntrySO를 추가합니다.")))
                    AddSelectedAssets();

                if (GUILayout.Button(new GUIContent("프로젝트 동기화", "프로젝트의 모든 SoundEntrySO 중 누락된 에셋을 추가합니다.")))
                    SyncProjectEntries();

                if (GUILayout.Button(new GUIContent("Key 정렬", "유효 key 기준으로 목록을 정렬합니다.")))
                    SortEntriesByKey();

                if (GUILayout.Button(new GUIContent("빈 참조 제거", "null 항목을 목록에서 제거합니다.")))
                    RemoveNullEntries();
            }
        }

        private void DrawDropArea()
        {
            Rect rect = GUILayoutUtility.GetRect(0f, 38f, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "SoundEntry 에셋을 여기에 드롭하여 추가", EditorStyles.helpBox);

            UnityEngine.Event evt = UnityEngine.Event.current;
            if (!rect.Contains(evt.mousePosition))
                return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDrop.objectReferences.Any(obj => obj is SoundEntrySO)
                    ? DragAndDropVisualMode.Copy
                    : DragAndDropVisualMode.Rejected;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                AddEntries(DragAndDrop.objectReferences.OfType<SoundEntrySO>());
                evt.Use();
            }
        }

        private void DrawTable()
        {
            DrawTableHeader();

            if (_visibleIndices.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    _entriesProperty.arraySize == 0
                        ? "등록된 항목이 없습니다."
                        : "현재 검색/필터 조건에 맞는 항목이 없습니다.",
                    MessageType.Info);
                return;
            }

            float height = Mathf.Min(ListMaxHeight, _visibleIndices.Count * (EditorGUIUtility.singleLineHeight + 4f) + 4f);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(height));

            for (int visibleRow = 0; visibleRow < _visibleIndices.Count; visibleRow++)
            {
                int index = _visibleIndices[visibleRow];
                DrawEntryRow(index, visibleRow);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawTableHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Space(StatusWidth);
                GUILayout.Label("Key / Asset", EditorStyles.miniBoldLabel, GUILayout.MinWidth(140f));
                GUILayout.Label("Bus", EditorStyles.miniBoldLabel, GUILayout.Width(BusWidth));
                GUILayout.Label("Clip", EditorStyles.miniBoldLabel, GUILayout.MinWidth(100f));
                GUILayout.Space(ActionWidth * 2f + 8f);
            }
        }

        private void DrawEntryRow(int index, int visibleRow)
        {
            SerializedProperty element = _entriesProperty.GetArrayElementAtIndex(index);
            var entry = element.objectReferenceValue as SoundEntrySO;
            bool selected = entry != null && entry == _selectedEntry;

            Color original = GUI.backgroundColor;
            if (selected)
                GUI.backgroundColor = new Color(0.45f, 0.7f, 1f, 0.45f);
            else if ((visibleRow & 1) == 1)
                GUI.backgroundColor = new Color(0.82f, 0.82f, 0.82f, 0.18f);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUI.backgroundColor = original;

                ValidationSeverity severity = GetEntrySeverity(index);
                GUILayout.Label(GetStatusIcon(severity), GUILayout.Width(StatusWidth));

                string key = ResolveKey(entry);
                if (GUILayout.Button(
                        new GUIContent(entry != null ? key : "(비어 있음)", entry != null ? entry.name : "null"),
                        EditorStyles.label,
                        GUILayout.MinWidth(140f)))
                {
                    SelectEntry(entry);
                }

                GUILayout.Label(entry != null ? entry.bus.ToString() : "-", GUILayout.Width(BusWidth));
                GUILayout.Label(
                    entry != null && entry.clip != null ? entry.clip.name : "(Clip 없음)",
                    entry != null && entry.clip == null ? EditorStyles.miniBoldLabel : EditorStyles.miniLabel,
                    GUILayout.MinWidth(100f));

                using (new EditorGUI.DisabledScope(entry == null))
                {
                    if (GUILayout.Button("선택", EditorStyles.miniButtonLeft, GUILayout.Width(ActionWidth)))
                    {
                        Selection.activeObject = entry;
                        EditorGUIUtility.PingObject(entry);
                        SelectEntry(entry);
                    }
                }

                if (GUILayout.Button("제거", EditorStyles.miniButtonRight, GUILayout.Width(ActionWidth)))
                {
                    RemoveAt(index);
                    GUIUtility.ExitGUI();
                }
            }

            GUI.backgroundColor = original;
        }

        private void DrawSelectedEntry()
        {
            if (_selectedEntry == null)
                return;

            EditorGUILayout.Space(6f);
            _showDetails = EditorGUILayout.BeginFoldoutHeaderGroup(
                _showDetails,
                $"상세 편집 · {ResolveKey(_selectedEntry)}");

            if (_showDetails)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (_entryEditor == null || _entryEditor.target != _selectedEntry)
                    {
                        DestroyEntryEditor();
                        _entryEditor = CreateEditor(_selectedEntry);
                    }

                    _entryEditor.OnInspectorGUI();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Project에서 찾기"))
                        {
                            Selection.activeObject = _selectedEntry;
                            EditorGUIUtility.PingObject(_selectedEntry);
                        }

                        if (GUILayout.Button("에셋 열기"))
                            AssetDatabase.OpenAsset(_selectedEntry);
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawValidation()
        {
            CountValidation(out int errorCount, out int warningCount);
            EditorGUILayout.Space(6f);
            _showValidation = EditorGUILayout.BeginFoldoutHeaderGroup(
                _showValidation,
                $"검증 결과 · 오류 {errorCount} / 경고 {warningCount}");

            if (_showValidation)
            {
                if (_messages.Count == 0)
                {
                    EditorGUILayout.HelpBox("SoundDatabase 검증 통과.", MessageType.Info);
                }
                else
                {
                    foreach (var message in _messages)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.HelpBox(message.Text, message.Type);
                            if (message.Entry != null && GUILayout.Button("찾기", GUILayout.Width(44f), GUILayout.Height(38f)))
                            {
                                SelectEntry(message.Entry);
                                _listScroll.y = Mathf.Max(0f, message.Index * 22f);
                            }
                        }
                    }
                }

                if (GUILayout.Button("검증 결과 Console 출력"))
                    LogValidationResult((SoundDatabaseSO)target, _messages);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void BuildVisibleIndices()
        {
            _visibleIndices.Clear();
            string normalizedSearch = _searchText?.Trim();

            for (int i = 0; i < _entriesProperty.arraySize; i++)
            {
                var entry = _entriesProperty.GetArrayElementAtIndex(i).objectReferenceValue as SoundEntrySO;

                if (_busFilter != BusFilter.전체 &&
                    (entry == null || entry.bus != (SoundBusType)_busFilter))
                    continue;

                if (_issuesOnly && GetEntrySeverity(i) == ValidationSeverity.None)
                    continue;

                if (!string.IsNullOrWhiteSpace(normalizedSearch))
                {
                    string key = ResolveKey(entry);
                    string assetName = entry != null ? entry.name : string.Empty;
                    string clipName = entry != null && entry.clip != null ? entry.clip.name : string.Empty;

                    if (key.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0 &&
                        assetName.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0 &&
                        clipName.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }

                _visibleIndices.Add(i);
            }
        }

        private void AddSelectedAssets()
        {
            AddEntries(Selection.objects.OfType<SoundEntrySO>());
        }

        private void SyncProjectEntries()
        {
            string[] guids = AssetDatabase.FindAssets("t:SoundEntrySO");
            var entries = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SoundEntrySO>)
                .Where(entry => entry != null)
                .OrderBy(ResolveKey, StringComparer.OrdinalIgnoreCase);

            AddEntries(entries);
        }

        private void AddEntries(IEnumerable<SoundEntrySO> entries)
        {
            var existing = new HashSet<SoundEntrySO>();
            for (int i = 0; i < _entriesProperty.arraySize; i++)
            {
                if (_entriesProperty.GetArrayElementAtIndex(i).objectReferenceValue is SoundEntrySO entry)
                    existing.Add(entry);
            }

            int added = 0;
            Undo.RecordObject(target, "SoundDatabase 항목 추가");
            foreach (var entry in entries.Distinct())
            {
                if (entry == null || !existing.Add(entry))
                    continue;

                int index = _entriesProperty.arraySize;
                _entriesProperty.InsertArrayElementAtIndex(index);
                _entriesProperty.GetArrayElementAtIndex(index).objectReferenceValue = entry;
                added++;
            }

            if (added <= 0)
                return;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            RefreshValidation();
        }

        private void SortEntriesByKey()
        {
            var entries = new List<SoundEntrySO>(_entriesProperty.arraySize);
            for (int i = 0; i < _entriesProperty.arraySize; i++)
                entries.Add(_entriesProperty.GetArrayElementAtIndex(i).objectReferenceValue as SoundEntrySO);

            entries.Sort((a, b) =>
            {
                if (a == null) return b == null ? 0 : 1;
                if (b == null) return -1;
                return string.Compare(ResolveKey(a), ResolveKey(b), StringComparison.OrdinalIgnoreCase);
            });

            Undo.RecordObject(target, "SoundDatabase Key 정렬");
            for (int i = 0; i < entries.Count; i++)
                _entriesProperty.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private void RemoveNullEntries()
        {
            Undo.RecordObject(target, "SoundDatabase 빈 참조 제거");
            for (int i = _entriesProperty.arraySize - 1; i >= 0; i--)
            {
                if (_entriesProperty.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    _entriesProperty.DeleteArrayElementAtIndex(i);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            RefreshValidation();
        }

        private void RemoveAt(int index)
        {
            var removing = _entriesProperty.GetArrayElementAtIndex(index).objectReferenceValue as SoundEntrySO;
            Undo.RecordObject(target, "SoundDatabase 항목 제거");
            _entriesProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);

            if (removing != null && removing == _selectedEntry)
                SelectEntry(null);

            RefreshValidation();
        }

        private void SelectEntry(SoundEntrySO entry)
        {
            if (_selectedEntry == entry)
                return;

            _selectedEntry = entry;
            DestroyEntryEditor();
            Repaint();
        }

        private void DestroyEntryEditor()
        {
            if (_entryEditor == null)
                return;

            DestroyImmediate(_entryEditor);
            _entryEditor = null;
        }

        private void RefreshValidation()
        {
            Validate((SoundDatabaseSO)target, _messages);
        }

        private ValidationSeverity GetEntrySeverity(int index)
        {
            ValidationSeverity severity = ValidationSeverity.None;

            foreach (var message in _messages)
            {
                if (message.Index != index)
                    continue;

                if (message.Type == MessageType.Error)
                    return ValidationSeverity.Error;

                if (message.Type == MessageType.Warning)
                    severity = ValidationSeverity.Warning;
            }

            return severity;
        }

        private static string GetStatusIcon(ValidationSeverity severity)
        {
            return severity switch
            {
                ValidationSeverity.Error => "●",
                ValidationSeverity.Warning => "▲",
                _ => "✓"
            };
        }

        private void CountValidation(out int errorCount, out int warningCount)
        {
            errorCount = 0;
            warningCount = 0;

            foreach (var message in _messages)
            {
                if (message.Type == MessageType.Error) errorCount++;
                else if (message.Type == MessageType.Warning) warningCount++;
            }
        }

        private static string ResolveKey(SoundEntrySO entry)
        {
            if (entry == null)
                return string.Empty;

            return string.IsNullOrWhiteSpace(entry.key)
                ? entry.name ?? string.Empty
                : entry.key.Trim();
        }

        private static void LogValidationResult(SoundDatabaseSO database, List<ValidationMessage> messages)
        {
            if (messages.Count == 0)
            {
                Debug.Log($"[SoundDatabase] '{database.name}' Validate: 문제 없음.", database);
                return;
            }

            int errorCount = 0;
            int warningCount = 0;

            foreach (var message in messages)
            {
                if (message.Type == MessageType.Error)
                {
                    errorCount++;
                    Debug.LogError($"[SoundDatabase][Validate] {message.Text}", message.Entry != null ? message.Entry : database);
                }
                else
                {
                    warningCount++;
                    Debug.LogWarning($"[SoundDatabase][Validate] {message.Text}", message.Entry != null ? message.Entry : database);
                }
            }

            Debug.Log($"[SoundDatabase] '{database.name}' Validate 완료: Error {errorCount}개 / Warning {warningCount}개.", database);
        }

        private static void Validate(SoundDatabaseSO database, List<ValidationMessage> messages)
        {
            messages.Clear();
            if (database == null)
                return;

            var entries = database.Entries;
            var keyToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                string label = $"entries[{i}]";

                if (entry == null)
                {
                    messages.Add(Error(i, null, $"{label}: SoundEntrySO 참조가 비어 있습니다."));
                    continue;
                }

                string key = ResolveKey(entry);
                if (string.IsNullOrWhiteSpace(key))
                {
                    messages.Add(Error(i, entry, $"{label}: key와 에셋 이름이 모두 비어 있습니다."));
                }
                else if (keyToIndex.TryGetValue(key, out int firstIndex))
                {
                    messages.Add(Error(i, entry, $"{label}: key '{key}'가 entries[{firstIndex}]와 중복됩니다."));
                }
                else
                {
                    keyToIndex.Add(key, i);
                }

                if (entry.clip == null)
                    messages.Add(Error(i, entry, $"{label} '{key}': AudioClip이 비어 있습니다."));

                if (entry.distanceMode == SoundDistanceMode.Custom3D &&
                    (entry.customRolloff == null || entry.customRolloff.length == 0))
                {
                    messages.Add(Warning(i, entry, $"{label} '{key}': Custom3D인데 customRolloff가 비어 있습니다."));
                }

                if (entry.distanceMode != SoundDistanceMode.None2D)
                {
                    if (entry.minDistance <= 0f)
                        messages.Add(Warning(i, entry, $"{label} '{key}': minDistance는 0보다 커야 합니다."));

                    if (entry.maxDistance <= entry.minDistance)
                        messages.Add(Warning(i, entry, $"{label} '{key}': maxDistance는 minDistance보다 커야 합니다."));
                }

                if (entry.pitchMin <= 0f || entry.pitchMax <= 0f)
                    messages.Add(Warning(i, entry, $"{label} '{key}': pitchMin/pitchMax는 0보다 커야 합니다."));

                if (entry.maxSimultaneous < 0)
                    messages.Add(Warning(i, entry, $"{label} '{key}': maxSimultaneous가 음수입니다."));

                if (entry.cooldown < 0f)
                    messages.Add(Warning(i, entry, $"{label} '{key}': cooldown이 음수입니다."));

                if (entry.distanceMode == SoundDistanceMode.None2D && entry.preCullByMaxDistance)
                    messages.Add(Warning(i, entry, $"{label} '{key}': 2D 사운드에는 preCullByMaxDistance가 적용되지 않습니다."));
            }
        }

        private static ValidationMessage Error(int index, SoundEntrySO entry, string text)
            => new(index, entry, text, MessageType.Error);

        private static ValidationMessage Warning(int index, SoundEntrySO entry, string text)
            => new(index, entry, text, MessageType.Warning);

        private enum ValidationSeverity
        {
            None,
            Warning,
            Error
        }

        private readonly struct ValidationMessage
        {
            public readonly int Index;
            public readonly SoundEntrySO Entry;
            public readonly string Text;
            public readonly MessageType Type;

            public ValidationMessage(int index, SoundEntrySO entry, string text, MessageType type)
            {
                Index = index;
                Entry = entry;
                Text = text;
                Type = type;
            }
        }
    }
}
