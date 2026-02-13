using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public class MotionSetEditorWindow : EditorWindow
    {
        MotionSetAsset  _asset;
        MotionSetDrawer _drawer;
        Vector2         _scrollPos;

        [MenuItem("UPlayGround/모션 셋 에디터")]
        static void Open()
        {
            var window = GetWindow<MotionSetEditorWindow>();
            window.titleContent = new GUIContent("모션 셋 에디터");
            window.minSize      = new Vector2(600, 400);
            window.Show();
        }

        void OnEnable()
        {
            _drawer = new MotionSetDrawer(() => _asset, Repaint);

            // Selection이 MotionSetAsset이면 자동 바인딩
            TryBindFromSelection();
        }

        void OnSelectionChange()
        {
            TryBindFromSelection();
            Repaint();
        }

        void TryBindFromSelection()
        {
            if (Selection.activeObject is MotionSetAsset selected)
                SetAsset(selected);
        }

        void SetAsset(MotionSetAsset asset)
        {
            if (_asset == asset) return;
            _asset  = asset;
            _drawer = new MotionSetDrawer(() => _asset, Repaint);
        }

        void OnGUI()
        {
            DrawToolbar();

            if (_asset == null)
            {
                DrawEmptyState();
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {
                _drawer.DrawFullGUI(_asset.motionSet);

                if (GUI.changed)
                    EditorUtility.SetDirty(_asset);
            }
            EditorGUILayout.EndScrollView();
        }

        // ── 상단 툴바 ──
        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                EditorGUILayout.LabelField("에셋", GUILayout.Width(35));

                var newAsset = (MotionSetAsset)EditorGUILayout.ObjectField(
                    _asset, typeof(MotionSetAsset), false, GUILayout.Width(250));

                if (newAsset != _asset)
                    SetAsset(newAsset);

                GUILayout.FlexibleSpace();

                if (_asset != null && GUILayout.Button("선택", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    Selection.activeObject = _asset;
                    EditorGUIUtility.PingObject(_asset);
                }

                if (GUILayout.Button("새로 만들기", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    CreateNewAsset();
            }
            EditorGUILayout.EndHorizontal();
        }

        // ── 에셋 미선택 상태 ──
        void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginVertical();
                {
                    var style = new GUIStyle(EditorStyles.largeLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize  = 14,
                        normal    = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                    };
                    EditorGUILayout.LabelField("MotionSetAsset을 선택하거나", style);
                    EditorGUILayout.LabelField("새로 만들어 주세요.", style);

                    EditorGUILayout.Space(12);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    // 드래그 앤 드롭 영역
                    Rect dropRect = GUILayoutUtility.GetRect(260, 60);
                    GUI.Box(dropRect, "여기에 MotionSetAsset 드래그");
                    HandleDragDrop(dropRect);

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(8);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("새 MotionSet 에셋 생성", GUILayout.Width(200), GUILayout.Height(30)))
                        CreateNewAsset();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        // ── 드래그 앤 드롭 ──
        void HandleDragDrop(Rect rect)
        {
            Event e = Event.current;
            if ((e.type == EventType.DragUpdated || e.type == EventType.DragPerform) && rect.Contains(e.mousePosition))
            {
                bool hasValid = false;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is MotionSetAsset) { hasValid = true; break; }
                }

                if (hasValid)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;

                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (obj is MotionSetAsset asset)
                            {
                                SetAsset(asset);
                                break;
                            }
                        }
                    }
                }
                e.Use();
            }
        }

        // ── 새 에셋 생성 ──
        void CreateNewAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "MotionSet 에셋 생성", "NewMotionSet", "asset", "저장 위치를 선택하세요.");

            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<MotionSetAsset>();
            asset.motionSet = new MotionSet { motionSetName = System.IO.Path.GetFileNameWithoutExtension(path) };

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SetAsset(asset);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}