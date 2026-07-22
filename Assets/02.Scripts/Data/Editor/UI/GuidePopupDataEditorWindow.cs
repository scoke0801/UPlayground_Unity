#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using UPlayGround.Data.UI;

namespace UPlayGround.Data.UI.EditorTools
{
    /// <summary>
    /// GuidePopupDataSO를 한 창에서 생성/편집하는 에디터 툴.
    /// </summary>
    public sealed class GuidePopupDataEditorWindow : EditorWindow
    {
        private const string DefaultSavePath = "Assets/10.Datas/Guide";
        private const float ListWidth = 260f;
        private const float PreviewSize = 120f;

        private readonly List<GuidePopupDataSO> _assets = new();

        private GuidePopupDataSO _selected;
        private SerializedObject _serializedTarget;
        private SerializedProperty _pagesProp;

        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private string _search = string.Empty;

        private GUIStyle _selectedRowStyle;
        private GUIStyle _normalRowStyle;
        private GUIStyle _pageHeaderStyle;
        private bool _stylesInitialized;

        public static void Open()
        {
            var window = GetWindow<GuidePopupDataEditorWindow>();
            window.titleContent = new GUIContent("Guide Popup Data", EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
            window.minSize = new Vector2(860f, 560f);
            window.Show();
        }

        public static void Open(GuidePopupDataSO data)
        {
            Open();
            var window = GetWindow<GuidePopupDataEditorWindow>();
            window.RefreshAssetList();
            window.SelectAsset(data);
        }

        private void OnEnable()
        {
            RefreshAssetList();
            if (_selected == null && Selection.activeObject is GuidePopupDataSO selected)
                SelectAsset(selected);
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is GuidePopupDataSO data)
            {
                SelectAsset(data);
                Repaint();
            }
        }

        private void OnGUI()
        {
            InitStyles();
            DrawToolbar();

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            DrawListPanel();
            DrawSplitter();
            DrawDetailPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void InitStyles()
        {
            if (_stylesInitialized)
                return;

            _stylesInitialized = true;
            _selectedRowStyle = new GUIStyle("SelectionRect")
            {
                padding = new RectOffset(8, 8, 5, 5),
                margin = new RectOffset(2, 2, 1, 1),
                fixedHeight = 42f
            };
            _normalRowStyle = new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 5, 5),
                margin = new RectOffset(2, 2, 1, 1),
                fixedHeight = 42f
            };
            _pageHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("새 데이터", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                CreateNewAsset();

            using (new EditorGUI.DisabledScope(_selected == null))
            {
                if (GUILayout.Button("복제", EditorStyles.toolbarButton, GUILayout.Width(48f)))
                    DuplicateSelectedAsset();

                if (GUILayout.Button("Project에서 보기", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                    PingSelected();
            }

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                RefreshAssetList();

            GUILayout.FlexibleSpace();
            GUILayout.Label("검색", EditorStyles.miniLabel, GUILayout.Width(28f));
            _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(180f));

            if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(22f)))
            {
                _search = string.Empty;
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ListWidth), GUILayout.ExpandHeight(true));

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"GuidePopupDataSO ({_assets.Count})", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            string lower = _search.ToLowerInvariant();

            foreach (var asset in _assets)
            {
                if (asset == null)
                    continue;

                if (!string.IsNullOrEmpty(lower) && !asset.name.ToLowerInvariant().Contains(lower))
                    continue;

                bool selected = asset == _selected;
                EditorGUILayout.BeginVertical(selected ? _selectedRowStyle : _normalRowStyle);
                GUILayout.Label(asset.name, selected ? EditorStyles.whiteBoldLabel : EditorStyles.boldLabel);
                GUILayout.Label($"{asset.Pages.Count} 페이지", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                Rect rect = GUILayoutUtility.GetLastRect();
                if (UnityEngine.Event.current.type == EventType.MouseDown && rect.Contains(UnityEngine.Event.current.mousePosition))
                {
                    SelectAsset(asset);
                    UnityEngine.Event.current.Use();
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private static void DrawSplitter()
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 0f, GUILayout.Width(1f), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f));
        }

        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (_selected == null || _serializedTarget == null || _pagesProp == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("좌측에서 가이드 팝업 데이터를 선택하거나 새 데이터를 생성하세요.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            _serializedTarget.Update();

            DrawDetailHeader();
            DrawValidationSummary();

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            DrawPages();
            EditorGUILayout.EndScrollView();

            DrawDetailFooter();

            if (_serializedTarget.ApplyModifiedProperties())
                EditorUtility.SetDirty(_selected);

            EditorGUILayout.EndVertical();
        }

        private void DrawDetailHeader()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(_selected.name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(AssetDatabase.GetAssetPath(_selected), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("페이지 수", _pagesProp.arraySize.ToString(), GUILayout.Width(140f));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+ 페이지 추가", GUILayout.Width(110f), GUILayout.Height(22f)))
                AddPage();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawValidationSummary()
        {
            int missingMedia = 0;
            int emptyText = 0;

            for (int i = 0; i < _pagesProp.arraySize; i++)
            {
                SerializedProperty page = _pagesProp.GetArrayElementAtIndex(i);
                bool isVideo = page.FindPropertyRelative("_mediaType").enumValueIndex == (int)GuidePopupMediaType.Video;
                bool hasMedia = isVideo
                    ? page.FindPropertyRelative("_video").objectReferenceValue != null
                    : page.FindPropertyRelative("_image").objectReferenceValue != null;

                if (!hasMedia)
                    missingMedia++;

                string title = page.FindPropertyRelative("_title").stringValue;
                string body = page.FindPropertyRelative("_body").stringValue;
                if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
                    emptyText++;
            }

            if (missingMedia == 0 && emptyText == 0)
                return;

            string message = $"확인 필요: 미디어 누락 {missingMedia}개, 제목/본문 공백 {emptyText}개";
            EditorGUILayout.HelpBox(message, MessageType.Warning);
        }

        private void DrawPages()
        {
            if (_pagesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("아직 페이지가 없습니다. '+ 페이지 추가'로 첫 페이지를 생성하세요.", MessageType.Info);
                return;
            }

            int removeAt = -1;
            int duplicateAt = -1;
            int moveFrom = -1;
            int moveTo = -1;

            for (int i = 0; i < _pagesProp.arraySize; i++)
            {
                SerializedProperty page = _pagesProp.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical("box");

                DrawPageHeader(i, ref removeAt, ref duplicateAt, ref moveFrom, ref moveTo);
                EditorGUILayout.Space(2f);
                DrawPageBody(page);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }

            if (duplicateAt >= 0)
                DuplicatePage(duplicateAt);

            if (moveFrom >= 0 && moveTo >= 0)
                _pagesProp.MoveArrayElement(moveFrom, moveTo);

            if (removeAt >= 0 && EditorUtility.DisplayDialog("페이지 삭제", $"{removeAt + 1}번 페이지를 삭제하시겠습니까?", "삭제", "취소"))
                _pagesProp.DeleteArrayElementAtIndex(removeAt);
        }

        private void DrawPageHeader(int index, ref int removeAt, ref int duplicateAt, ref int moveFrom, ref int moveTo)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"페이지 {index + 1}", _pageHeaderStyle);
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(index <= 0))
            {
                if (GUILayout.Button("위", EditorStyles.toolbarButton, GUILayout.Width(32f)))
                {
                    moveFrom = index;
                    moveTo = index - 1;
                }
            }

            using (new EditorGUI.DisabledScope(index >= _pagesProp.arraySize - 1))
            {
                if (GUILayout.Button("아래", EditorStyles.toolbarButton, GUILayout.Width(42f)))
                {
                    moveFrom = index;
                    moveTo = index + 1;
                }
            }

            if (GUILayout.Button("복제", EditorStyles.toolbarButton, GUILayout.Width(42f)))
                duplicateAt = index;

            if (GUILayout.Button("삭제", EditorStyles.toolbarButton, GUILayout.Width(42f)))
                removeAt = index;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPageBody(SerializedProperty page)
        {
            SerializedProperty mediaType = page.FindPropertyRelative("_mediaType");
            SerializedProperty image = page.FindPropertyRelative("_image");
            SerializedProperty video = page.FindPropertyRelative("_video");
            SerializedProperty loopVideo = page.FindPropertyRelative("_loopVideo");
            SerializedProperty title = page.FindPropertyRelative("_title");
            SerializedProperty body = page.FindPropertyRelative("_body");

            EditorGUILayout.BeginHorizontal();
            DrawMediaPreview(mediaType, image, video);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(mediaType, new GUIContent("미디어 타입"));

            bool isVideo = mediaType.enumValueIndex == (int)GuidePopupMediaType.Video;
            if (isVideo)
            {
                EditorGUILayout.PropertyField(video, new GUIContent("동영상"));
                EditorGUILayout.PropertyField(loopVideo, new GUIContent("반복 재생"));
            }
            else
            {
                EditorGUILayout.PropertyField(image, new GUIContent("이미지"));
            }

            EditorGUILayout.PropertyField(title, new GUIContent("제목"));
            EditorGUILayout.LabelField("본문");
            body.stringValue = EditorGUILayout.TextArea(body.stringValue, GUILayout.MinHeight(72f));
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawMediaPreview(SerializedProperty mediaType, SerializedProperty image, SerializedProperty video)
        {
            Rect rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.Width(PreviewSize), GUILayout.Height(PreviewSize));
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.08f, 0.08f));

            bool isVideo = mediaType.enumValueIndex == (int)GuidePopupMediaType.Video;
            UnityEngine.Object target = isVideo ? video.objectReferenceValue : image.objectReferenceValue;
            Texture preview = GetPreviewTexture(target);

            if (preview != null)
            {
                GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
                return;
            }

            string label = isVideo ? "Video" : "Image";
            GUI.Label(rect, label, EditorStyles.centeredGreyMiniLabel);
        }

        private static Texture GetPreviewTexture(UnityEngine.Object target)
        {
            if (target == null)
                return null;

            if (target is Sprite sprite)
                return AssetPreview.GetAssetPreview(sprite) ?? sprite.texture;

            if (target is VideoClip)
                return AssetPreview.GetAssetPreview(target) ?? AssetPreview.GetMiniThumbnail(target);

            return AssetPreview.GetMiniThumbnail(target);
        }

        private void DrawDetailFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("+ 페이지 추가", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                AddPage();

            using (new EditorGUI.DisabledScope(_pagesProp.arraySize == 0))
            {
                if (GUILayout.Button("빈 페이지 정리", EditorStyles.toolbarButton, GUILayout.Width(92f)))
                    RemoveEmptyPages();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("저장", EditorStyles.toolbarButton, GUILayout.Width(54f)))
            {
                _serializedTarget.ApplyModifiedProperties();
                EditorUtility.SetDirty(_selected);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshAssetList()
        {
            _assets.Clear();

            string[] guids = AssetDatabase.FindAssets("t:GuidePopupDataSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GuidePopupDataSO>(path);
                if (asset != null)
                    _assets.Add(asset);
            }

            _assets.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        }

        private void SelectAsset(GuidePopupDataSO asset)
        {
            _selected = asset;
            _serializedTarget = asset != null ? new SerializedObject(asset) : null;
            _pagesProp = _serializedTarget?.FindProperty("_pages");
        }

        private void CreateNewAsset()
        {
            EnsureFolder(DefaultSavePath);

            string path = EditorUtility.SaveFilePanelInProject(
                "가이드 팝업 데이터 생성",
                "GuidePopupData_New",
                "asset",
                "저장 위치를 선택하세요.",
                DefaultSavePath);

            if (string.IsNullOrEmpty(path))
                return;

            var asset = CreateInstance<GuidePopupDataSO>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshAssetList();
            SelectAsset(asset);
            EditorGUIUtility.PingObject(asset);
        }

        private void DuplicateSelectedAsset()
        {
            if (_selected == null)
                return;

            string sourcePath = AssetDatabase.GetAssetPath(_selected);
            string targetPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);

            if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                EditorUtility.DisplayDialog("복제 실패", "선택한 가이드 팝업 데이터를 복제하지 못했습니다.", "확인");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var duplicated = AssetDatabase.LoadAssetAtPath<GuidePopupDataSO>(targetPath);
            RefreshAssetList();
            SelectAsset(duplicated);
            EditorGUIUtility.PingObject(duplicated);
        }

        private void AddPage()
        {
            int index = _pagesProp.arraySize;
            _pagesProp.InsertArrayElementAtIndex(index);
            ResetPage(_pagesProp.GetArrayElementAtIndex(index));
        }

        private void DuplicatePage(int index)
        {
            if (index < 0 || index >= _pagesProp.arraySize)
                return;

            _pagesProp.InsertArrayElementAtIndex(index);
            _pagesProp.MoveArrayElement(index, index + 1);
        }

        private void RemoveEmptyPages()
        {
            if (!EditorUtility.DisplayDialog("빈 페이지 정리", "미디어, 제목, 본문이 모두 비어 있는 페이지를 삭제하시겠습니까?", "정리", "취소"))
                return;

            for (int i = _pagesProp.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty page = _pagesProp.GetArrayElementAtIndex(i);
                bool hasImage = page.FindPropertyRelative("_image").objectReferenceValue != null;
                bool hasVideo = page.FindPropertyRelative("_video").objectReferenceValue != null;
                bool hasTitle = !string.IsNullOrWhiteSpace(page.FindPropertyRelative("_title").stringValue);
                bool hasBody = !string.IsNullOrWhiteSpace(page.FindPropertyRelative("_body").stringValue);

                if (!hasImage && !hasVideo && !hasTitle && !hasBody)
                    _pagesProp.DeleteArrayElementAtIndex(i);
            }
        }

        private static void ResetPage(SerializedProperty page)
        {
            page.FindPropertyRelative("_mediaType").enumValueIndex = (int)GuidePopupMediaType.Image;
            page.FindPropertyRelative("_image").objectReferenceValue = null;
            page.FindPropertyRelative("_video").objectReferenceValue = null;
            page.FindPropertyRelative("_loopVideo").boolValue = true;
            page.FindPropertyRelative("_title").stringValue = "가이드 제목";
            page.FindPropertyRelative("_body").stringValue = string.Empty;
        }

        private void PingSelected()
        {
            if (_selected == null)
                return;

            Selection.activeObject = _selected;
            EditorGUIUtility.PingObject(_selected);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }

    [CustomEditor(typeof(GuidePopupDataSO))]
    public sealed class GuidePopupDataSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("데이터 저작 허브에서 열기", GUILayout.Height(28f)))
                UPlayGround.Data.Editor.Authoring.DataAuthoringHubWindow.Open(
                    UPlayGround.Data.Editor.Authoring.GuidePopupDomainPanel.DomainKey,
                    target);

            if (GUILayout.Button("미디어 미리보기 편집기 열기", GUILayout.Height(24f)))
                GuidePopupDataEditorWindow.Open((GuidePopupDataSO)target);

            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
        }
    }
}
#endif
