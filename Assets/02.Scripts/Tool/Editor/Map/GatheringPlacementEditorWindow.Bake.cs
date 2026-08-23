#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UPlayGround.Data.EnumType;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.World;
using UPlayGround.Group;
using UPlayGround.Data.Item;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>RuntimeData Bake 액션과 Bake 데이터 뷰어.</summary>
    public partial class GatheringPlacementEditorWindow
    {
        /// <summary>우측 하단 고정: 고급 기능인 RuntimeData Bake 영역. 실수 방지를 위해 경고색으로 구분.</summary>
        private void DrawRuntimeDataActions()
        {
            Rect rect = EditorGUILayout.BeginVertical();
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(0.16f, 0.15f, 0.12f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(0.42f, 0.35f, 0.17f));
            }

            GUILayout.Space(4f);

            EditorGUI.BeginChangeCheck();
            _bakeFoldout = EditorGUILayout.Foldout(_bakeFoldout, "⚠ RuntimeData Bake (고급)", true, _bakeHeaderStyle);
            if (EditorGUI.EndChangeCheck())
                SaveFoldoutPrefs();

            if (_bakeFoldout)
            {
                GUILayout.Label(
                    "Bake Mode가 RuntimeData인 배치만 PlacementDataSO로 저장하고 씬 오브젝트를 제거합니다. 메타데이터가 없는 기존 씬 오브젝트는 먼저 등록할 수 있습니다.",
                    EditorStyles.wordWrappedMiniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(Selection.gameObjects == null || Selection.gameObjects.Length == 0))
                    {
                        if (GUILayout.Button("기존 선택 RuntimeData 등록", GUILayout.Height(22f)))
                            WorldPlacementBakeUtility.RegisterSelectedAsRuntimeData();

                        if (GUILayout.Button("기존 선택 등록 후 Bake", GUILayout.Height(22f)))
                        {
                            var baked = WorldPlacementBakeUtility.RegisterSelectedAndBakeRuntimeData();
                            RefreshBakedDataAssets();
                            if (baked != null)
                                _selectedBakedData = baked;
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Bake Open Scene", GUILayout.Height(24f)))
                    {
                        var baked = WorldPlacementBakeUtility.BakeOpenSceneRuntimeData();
                        RefreshBakedDataAssets();
                        if (baked != null)
                            _selectedBakedData = baked;
                    }

                    bool canRestore = Selection.activeObject is WorldPlacementDataSO;
                    using (new EditorGUI.DisabledScope(!canRestore))
                    {
                        if (GUILayout.Button(
                                new GUIContent("Restore Selected Data", canRestore ? "" : "WorldPlacementDataSO 에셋을 선택하면 활성화됩니다."),
                                GUILayout.Height(24f)))
                            WorldPlacementBakeUtility.RestoreSelectedPlacementData();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(Selection.gameObjects == null || Selection.gameObjects.Length == 0))
                    {
                        if (GUILayout.Button("선택 RuntimeData 표시", GUILayout.Height(22f)))
                            WorldPlacementBakeUtility.MarkSelectedAsRuntimeData();

                        if (GUILayout.Button("선택 SceneObject 표시", GUILayout.Height(22f)))
                            WorldPlacementBakeUtility.MarkSelectedAsSceneObject();
                    }
                }

                if (Selection.activeObject is not WorldPlacementDataSO)
                    GUILayout.Label("※ Restore는 WorldPlacementDataSO 에셋 선택 시 활성화됩니다.", EditorStyles.miniLabel);

                DrawBakedDataViewer();
            }

            GUILayout.Space(4f);
            EditorGUILayout.EndVertical();
        }

        /// <summary>Bake된 WorldPlacementDataSO의 레코드 목록을 확인하고 씬에서 위치를 표시/이동한다.</summary>
        private void DrawBakedDataViewer()
        {
            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Bake된 데이터", EditorStyles.miniBoldLabel, GUILayout.Width(84f));

            string[] options = BuildBakedDataOptions(out int current);
            int picked = EditorGUILayout.Popup(current, options);
            if (picked != current)
            {
                _selectedBakedData = picked <= 0 || picked > _bakedDataAssets.Count ? null : _bakedDataAssets[picked - 1];
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            if (_selectedBakedData == null)
                return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"레코드 {_selectedBakedData.Records.Count}개", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            _showBakedInScene = GUILayout.Toggle(_showBakedInScene, "씬에 표시", EditorStyles.miniButton, GUILayout.Width(62f));
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();

            if (GUILayout.Button("에셋 선택", EditorStyles.miniButton, GUILayout.Width(62f)))
            {
                Selection.activeObject = _selectedBakedData;
                EditorGUIUtility.PingObject(_selectedBakedData);
            }

            if (GUILayout.Button(
                    new GUIContent("비우기", "레코드를 모두 제거한다. 에셋과 씬 로더 연결은 유지된다."),
                    EditorStyles.miniButton, GUILayout.Width(52f)))
                RequestBakedDataAction(data => WorldPlacementBakeUtility.ClearRecords(data));

            if (GUILayout.Button(
                    new GUIContent("에셋 삭제", "PlacementData 에셋 자체를 삭제하고 열린 씬의 로더 참조를 해제한다."),
                    EditorStyles.miniButton, GUILayout.Width(62f)))
                RequestBakedDataAction(data => WorldPlacementBakeUtility.DeletePlacementDataAsset(data));
            EditorGUILayout.EndHorizontal();

            _bakedListScroll = EditorGUILayout.BeginScrollView(_bakedListScroll, GUILayout.Height(120f));
            var records = _selectedBakedData.Records;
            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (record == null)
                    continue;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"{i + 1}. {WorldPlacementBakeUtility.GetRecordDisplayName(record)}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(
                    $"({record.position.x:F1}, {record.position.y:F1}, {record.position.z:F1})",
                    EditorStyles.miniLabel, GUILayout.Width(120f));
                if (GUILayout.Button("이동", EditorStyles.miniButton, GUILayout.Width(36f)))
                    FrameSceneView(record.position);

                WorldPlacementRecord removeTarget = record;
                if (GUILayout.Button(
                        new GUIContent("×", "이 레코드를 Bake 데이터에서 제거한다."),
                        EditorStyles.miniButton, GUILayout.Width(22f)))
                    RequestBakedDataAction(data => WorldPlacementBakeUtility.RemoveRecord(data, removeTarget));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Bake 데이터 제거 액션을 다음 프레임으로 미뤄 실행한다.
        /// 버튼 처리 중에 레코드 수가 바뀌거나 확인 다이얼로그가 뜨면 이번 프레임의 IMGUI 레이아웃이 어긋난다.
        /// </summary>
        private void RequestBakedDataAction(Func<WorldPlacementDataSO, bool> action)
        {
            WorldPlacementDataSO target = _selectedBakedData;
            if (target == null)
                return;

            EditorApplication.delayCall += () =>
            {
                if (target == null || !action(target))
                    return;

                RefreshBakedDataAssets();
                SceneView.RepaintAll();
                Repaint();
            };
            GUIUtility.ExitGUI();
        }

        private string[] BuildBakedDataOptions(out int currentIndex)
        {
            var options = new string[_bakedDataAssets.Count + 1];
            options[0] = _bakedDataAssets.Count == 0 ? "(Bake된 데이터 없음)" : "(선택 안 함)";
            currentIndex = 0;
            for (int i = 0; i < _bakedDataAssets.Count; i++)
            {
                options[i + 1] = _bakedDataAssets[i].name;
                if (_bakedDataAssets[i] == _selectedBakedData)
                    currentIndex = i + 1;
            }

            return options;
        }

        private void RefreshBakedDataAssets()
        {
            _bakedDataAssets.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:WorldPlacementDataSO"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<WorldPlacementDataSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    _bakedDataAssets.Add(asset);
            }

            _bakedDataAssets.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            if (_selectedBakedData != null && !_bakedDataAssets.Contains(_selectedBakedData))
                _selectedBakedData = null;
        }

        private static void FrameSceneView(Vector3 position)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
                sceneView.Frame(new Bounds(position, Vector3.one * 6f), false);
        }

    }
}
#endif
