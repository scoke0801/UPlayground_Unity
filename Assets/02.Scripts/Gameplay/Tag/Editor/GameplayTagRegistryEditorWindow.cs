using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UPlayGround.Gameplay.Tag.Editor
{
    /// <summary>
    /// 코드 생성 없이 GameplayTagRegistrySO 데이터만 편집하는 창.
    /// </summary>
    public sealed class GameplayTagRegistryEditorWindow : EditorWindow
    {
        private const string RegistryPath =
            "Assets/Resources/GameplayTagRegistry.asset";

        private GameplayTagRegistrySO _registry;
        private SerializedObject _serializedObject;
        private SerializedProperty _tags;
        private ReorderableList _list;
        private Vector2 _scroll;
        private string _status = string.Empty;
        private bool _statusIsError;

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/게임플레이/게임플레이 태그/태그 레지스트리 에디터")]
        public static void Open()
        {
            GameplayTagRegistryEditorWindow window =
                GetWindow<GameplayTagRegistryEditorWindow>();
            window.titleContent = new GUIContent("GameplayTag Registry");
            window.minSize = new Vector2(720f, 460f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadRegistry();
        }

        private void LoadRegistry()
        {
            _registry = AssetDatabase.LoadAssetAtPath<GameplayTagRegistrySO>(
                RegistryPath);
            if (_registry == null)
            {
                string[] guids =
                    AssetDatabase.FindAssets("t:GameplayTagRegistrySO");
                if (guids.Length > 0)
                {
                    _registry =
                        AssetDatabase.LoadAssetAtPath<GameplayTagRegistrySO>(
                            AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            if (_registry == null) return;
            _serializedObject = new SerializedObject(_registry);
            _tags = _serializedObject.FindProperty("tags");
            BuildList();
        }

        private void BuildList()
        {
            _list = new ReorderableList(
                _serializedObject,
                _tags,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true)
            {
                elementHeight = EditorGUIUtility.singleLineHeight * 3f + 12f,
            };

            _list.drawHeaderCallback = rect =>
                EditorGUI.LabelField(
                    rect,
                    $"등록 태그 ({_tags.arraySize})",
                    EditorStyles.boldLabel);

            _list.drawElementCallback = (
                rect,
                index,
                isActive,
                isFocused) =>
            {
                SerializedProperty element =
                    _tags.GetArrayElementAtIndex(index);
                SerializedProperty name =
                    element.FindPropertyRelative("tagName");
                SerializedProperty description =
                    element.FindPropertyRelative("description");
                SerializedProperty color =
                    element.FindPropertyRelative("color");

                float line = EditorGUIUtility.singleLineHeight;
                rect.y += 3f;
                Rect colorRect = new(rect.x, rect.y, 52f, line);
                Rect nameRect = new(
                    colorRect.xMax + 4f,
                    rect.y,
                    rect.width - colorRect.width - 4f,
                    line);
                Rect renameRect = nameRect;
                renameRect.xMin = renameRect.xMax - 54f;
                nameRect.xMax = renameRect.xMin - 4f;
                Rect descriptionRect = new(
                    rect.x,
                    rect.y + line + 3f,
                    rect.width,
                    line);
                Rect pathRect = new(
                    rect.x,
                    rect.y + (line + 3f) * 2f,
                    rect.width,
                    line);

                color.colorValue = EditorGUI.ColorField(
                    colorRect,
                    GUIContent.none,
                    color.colorValue,
                    showEyedropper: true,
                    showAlpha: false,
                    hdr: false);
                EditorGUI.SelectableLabel(
                    nameRect,
                    name.stringValue,
                    EditorStyles.textField);
                if (GUI.Button(renameRect, "Rename"))
                {
                    string selectedName = name.stringValue;
                    EditorApplication.delayCall += () =>
                        GameplayTagRenameWindow.Open(selectedName);
                }
                description.stringValue = EditorGUI.TextField(
                    descriptionRect,
                    "설명",
                    description.stringValue);
                EditorGUI.LabelField(
                    pathRect,
                    string.IsNullOrWhiteSpace(name.stringValue)
                        ? "태그 이름을 입력하세요."
                        : $"선택 UI 경로: {name.stringValue.Replace('.', '/')}",
                    EditorStyles.miniLabel);
            };

            _list.onAddCallback = list =>
            {
                int index = _tags.arraySize;
                _tags.InsertArrayElementAtIndex(index);
                SerializedProperty element =
                    _tags.GetArrayElementAtIndex(index);
                string newTagName = FindAvailableNewTagName();
                element.FindPropertyRelative("tagName").stringValue =
                    newTagName;
                element.FindPropertyRelative("description").stringValue =
                    string.Empty;
                element.FindPropertyRelative("color").colorValue =
                    new Color(0.4f, 0.8f, 1f);
                _serializedObject.ApplyModifiedProperties();
                _registry.RebuildLookup();
                EditorUtility.SetDirty(_registry);
                AssetDatabase.SaveAssetIfDirty(_registry);
                list.index = index;
                EditorApplication.delayCall += () =>
                    GameplayTagRenameWindow.Open(newTagName);
            };
        }

        private void OnGUI()
        {
            DrawToolbar();
            if (_registry == null)
            {
                EditorGUILayout.HelpBox(
                    $"{RegistryPath}에 Registry 에셋이 없습니다.",
                    MessageType.Error);
                return;
            }

            _serializedObject.Update();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _list.DoLayoutList();
            EditorGUILayout.EndScrollView();

            if (_serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_registry);
                _registry.RebuildLookup();
            }

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.HelpBox(
                    _status,
                    _statusIsError ? MessageType.Error : MessageType.Info);
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(
                "Registry 데이터 편집 — 코드 생성 없음",
                EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!TryGetSelectedTagName(
                       out string selectedTagName)))
            {
                if (GUILayout.Button(
                        "사용처",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(52f)))
                {
                    GameplayTagReferenceWindow.Open(selectedTagName);
                }
                if (GUILayout.Button(
                        "이름 변경",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(65f)))
                {
                    GameplayTagRenameWindow.Open(selectedTagName);
                }
            }
            if (GUILayout.Button(
                    "검증",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(45f)))
                ValidateRegistry();
            if (GUILayout.Button(
                    "저장",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(45f)))
                SaveRegistry();
            EditorGUILayout.EndHorizontal();
        }

        private bool TryGetSelectedTagName(out string tagName)
        {
            tagName = string.Empty;
            if (_tags == null
                || _list == null
                || _list.index < 0
                || _list.index >= _tags.arraySize)
            {
                return false;
            }

            SerializedProperty element =
                _tags.GetArrayElementAtIndex(_list.index);
            tagName =
                element.FindPropertyRelative("tagName").stringValue;
            return !string.IsNullOrWhiteSpace(tagName);
        }

        private string FindAvailableNewTagName()
        {
            const string baseName = "New.Tag";
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _tags.arraySize; i++)
            {
                SerializedProperty element =
                    _tags.GetArrayElementAtIndex(i);
                names.Add(
                    element.FindPropertyRelative("tagName").stringValue);
            }

            if (!names.Contains(baseName))
                return baseName;
            for (int suffix = 2; ; suffix++)
            {
                string candidate = baseName + suffix;
                if (!names.Contains(candidate))
                    return candidate;
            }
        }

        private void ValidateRegistry()
        {
            _serializedObject.ApplyModifiedProperties();
            var names = new HashSet<string>(StringComparer.Ordinal);
            var errors = new List<string>();
            for (int i = 0; i < _registry.tags.Count; i++)
            {
                GameplayTagDefinition definition = _registry.tags[i];
                string tagName = definition?.tagName ?? string.Empty;
                if (string.IsNullOrWhiteSpace(tagName))
                    errors.Add($"{i}번 태그 이름이 비어 있습니다.");
                else if (!string.Equals(
                             tagName,
                             tagName.Trim(),
                             StringComparison.Ordinal))
                    errors.Add($"앞뒤 공백: \"{tagName}\"");
                else if (!names.Add(tagName))
                    errors.Add($"중복 태그: \"{tagName}\"");
            }

            _statusIsError = errors.Count > 0;
            _status = errors.Count == 0
                ? $"등록 태그 {_registry.tags.Count}개가 유효합니다."
                : string.Join("\n", errors);
        }

        private void SaveRegistry()
        {
            ValidateRegistry();
            if (_statusIsError) return;

            _registry.RebuildLookup();
            EditorUtility.SetDirty(_registry);
            AssetDatabase.SaveAssetIfDirty(_registry);
            _status = "Registry 데이터를 저장했습니다. 재컴파일은 필요하지 않습니다.";
        }
    }

    [CustomEditor(typeof(GameplayTagRegistrySO))]
    public sealed class GameplayTagRegistryInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var registry = (GameplayTagRegistrySO)target;
            EditorGUILayout.HelpBox(
                "태그 이름과 사용처의 정합성을 보호하기 위해 전용 "
                + "Registry 에디터에서 편집합니다. 이름 변경은 반드시 "
                + "Rename 기능을 사용하세요.",
                MessageType.Info);
            EditorGUILayout.LabelField(
                "등록 태그",
                registry.tags?.Count.ToString() ?? "0");

            if (GUILayout.Button("GameplayTag Registry 에디터 열기"))
                GameplayTagRegistryEditorWindow.Open();
            if (GUILayout.Button("등록 무결성 검증"))
                GameplayTagRegistryBuildValidator.ValidateFromMenu();
        }
    }
}
