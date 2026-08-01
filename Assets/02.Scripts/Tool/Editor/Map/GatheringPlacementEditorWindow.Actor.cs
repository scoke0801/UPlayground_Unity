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
using UPlayGround.Cycle;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>액터 배치 모드 UI: 목록, 필터, 몬스터 그룹, 배치 규칙, 소스 설정.</summary>
    public partial class GatheringPlacementEditorWindow
    {
        private void DrawActorListPanel()
        {
            if (_actorSource == ActorPlacementSource.GroupPreset)
            {
                DrawGroupPresetListPanel();
                return;
            }

            if (_actorSource == ActorPlacementSource.DirectPrefab)
            {
                EditorGUILayout.HelpBox(
                    "직접 프리팹 소스를 사용 중입니다.\n우측 '액터 배치 소스 설정'에서 프리팹을 연결하세요.",
                    MessageType.Info);
                GUILayout.FlexibleSpace();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName(SearchControlName);
            _actorSearchFilter = EditorGUILayout.TextField(_actorSearchFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                _actorSearchFilter = "";
            EditorGUILayout.EndHorizontal();

            DrawActorFilterChips();

            if (_actorDatabase == null)
            {
                EditorGUILayout.HelpBox(
                    "ActorDatabase를 연결해야 액터 목록을 사용할 수 있습니다.\n우측 '액터 배치 소스 설정'에서 연결하세요.",
                    MessageType.Warning);
                GUILayout.FlexibleSpace();
                return;
            }

            DrawActorDefinitionList();
        }

        private void DrawActorFilterChips()
        {
            EditorGUILayout.BeginHorizontal();
            DrawActorFilterChip(ActorType.Player, "Player");
            DrawActorFilterChip(ActorType.Monster, "Monster");
            DrawActorFilterChip(ActorType.NPC, "NPC");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActorFilterChip(ActorType flag, string label)
        {
            bool active = (_actorFilter & flag) != 0;
            Color previousBg = GUI.backgroundColor;
            GUI.backgroundColor = active ? new Color(0.5f, 0.75f, 0.55f) : new Color(0.35f, 0.35f, 0.35f);
            if (GUILayout.Button(label, _chipStyle, GUILayout.MaxWidth(80f)))
                _actorFilter = active ? _actorFilter & ~flag : _actorFilter | flag;
            GUI.backgroundColor = previousBg;
        }

        private void DrawActorDefinitionList()
        {
            _actorListScroll = EditorGUILayout.BeginScrollView(_actorListScroll, GUILayout.ExpandHeight(true));

            bool anyShown = false;
            foreach (var definition in _actorDefinitions)
            {
                if (!ShouldShowDefinition(definition))
                    continue;

                anyShown = true;
                DrawActorDefinitionRow(definition);
            }

            if (!anyShown)
                GUILayout.Label("표시할 ActorDefinitionSO가 없습니다.", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(32f));

            EditorGUILayout.EndScrollView();
        }

        private void DrawActorDefinitionRow(ActorDefinitionSO definition)
        {
            bool isSelected = _selectedActorDefinition == definition;
            Rect rect = GUILayoutUtility.GetRect(0f, 38f, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
                (isSelected ? _selectedItemStyle : _normalItemStyle).Draw(rect, GUIContent.none, false, false, isSelected, false);

            if (isSelected && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + 3f, 3f, rect.height - 6f), new Color(0.55f, 0.72f, 1f));

            string displayName = string.IsNullOrEmpty(definition.displayName) ? definition.actorId : definition.displayName;
            GUI.Label(new Rect(rect.x + 8f, rect.y + 4f, rect.width - 16f, 16f), displayName, EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 20f, rect.width - 16f, 14f),
                $"{definition.actorId}  |  {definition.actorType}", EditorStyles.miniLabel);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                SelectActorDefinition(definition);
                Event.current.Use();
            }
        }

        /// <summary>선택 항목과 무관하게 모든 액터 배치에 적용되는 공용 옵션.</summary>
        private void DrawActorCommonOptions()
        {
            DrawSectionLabel("공용 옵션");

            _parent = (Transform)EditorGUILayout.ObjectField("Parent", _parent, typeof(Transform), true);
            _autoCreateRoot = EditorGUILayout.Toggle("Auto Create Root", _autoCreateRoot);
            _selectAfterPlace = EditorGUILayout.Toggle("Select After Place", _selectAfterPlace);
            _addPlacementMetadata = EditorGUILayout.Toggle("Add Placement Metadata", _addPlacementMetadata);
            using (new EditorGUI.DisabledScope(!_addPlacementMetadata))
                _placementBakeMode = (WorldPlacementMetadata.PlacementBakeMode)EditorGUILayout.EnumPopup("Bake Mode", _placementBakeMode);
        }

        /// <summary>
        /// 몬스터 배치 시 소속 그룹 지정.
        /// MonsterGroupController는 자식 계층에서 멤버를 수집하므로, 그룹 지정 = 그룹 오브젝트 하위로 부모 지정이다.
        /// </summary>
        private void DrawMonsterGroupSection()
        {
            EditorGUILayout.Space(6f);
            DrawSectionLabel("몬스터 그룹");

            EditorGUILayout.BeginHorizontal();
            _targetGroup = (MonsterGroupController)EditorGUILayout.ObjectField("Group", _targetGroup, typeof(MonsterGroupController), true);
            if (GUILayout.Button("새 그룹", GUILayout.Width(56f)))
                CreateNewMonsterGroup();
            EditorGUILayout.EndHorizontal();

            DrawSceneGroupPopup();

            if (_targetGroup == null)
                return;

            var prefab = GetActorPrefab();
            if (prefab != null && prefab.GetComponent<MonsterActor>() == null)
                DrawInlineNotice("선택 프리팹에 MonsterActor가 없어 그룹 지정이 무시됩니다.", MessageType.Warning);
            else
                DrawInlineNotice($"배치되는 몬스터가 '{_targetGroup.name}' 하위로 들어가 그룹에 소속됩니다. Parent 옵션보다 우선합니다.", MessageType.Info);
        }

        private void DrawSceneGroupPopup()
        {
            var groups = FindSceneMonsterGroups();
            if (groups.Count == 0)
                return;

            var options = new string[groups.Count + 1];
            options[0] = "(그룹 없음)";
            int current = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                options[i + 1] = groups[i].name;
                if (groups[i] == _targetGroup)
                    current = i + 1;
            }

            int picked = EditorGUILayout.Popup("씬 그룹", current, options);
            if (picked != current)
                _targetGroup = picked <= 0 ? null : groups[picked - 1];
        }

        private static List<MonsterGroupController> FindSceneMonsterGroups()
        {
            var groups = new List<MonsterGroupController>(
                UnityEngine.Object.FindObjectsByType<MonsterGroupController>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            groups.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            return groups;
        }

        private void CreateNewMonsterGroup()
        {
            Transform parent = _parent != null ? _parent : GetOrCreatePlacementRoot(ActorPlacementRootName);

            var groupObject = new GameObject(GameObjectUtility.GetUniqueNameForSibling(parent, "MonsterGroup"));
            Undo.RegisterCreatedObjectUndo(groupObject, "Create Monster Group");
            if (parent != null)
                Undo.SetTransformParent(groupObject.transform, parent, "Create Monster Group Parent");

            // 씬 뷰 피벗 근처에 두어 하이어라키에서 찾기 쉽게 한다. 멤버 배치 좌표는 월드 기준이라 영향 없음.
            var sceneView = SceneView.lastActiveSceneView;
            groupObject.transform.position = sceneView != null ? sceneView.pivot : Vector3.zero;

            _targetGroup = Undo.AddComponent<MonsterGroupController>(groupObject);
            EnsureGroupSceneEntityId(groupObject);
            EditorSceneManager.MarkSceneDirty(groupObject.scene);
            SetTemporaryStatus($"'{groupObject.name}' 그룹을 생성했습니다.", MessageType.Info);
        }

        /// <summary>그룹 지정이 실제로 적용되는 상황인지. Actor 모드 + 그룹 선택 + 프리팹이 MonsterActor일 때만.</summary>
        private bool ShouldParentToGroup()
        {
            if (_worldPlacementMode != WorldPlacementMode.Actor || _targetGroup == null)
                return false;

            var prefab = GetActorPrefab();
            return IsMonsterActorPrefab(prefab);
        }

        private void DrawActorPlacementRules()
        {
            EditorGUILayout.Space(6f);
            EditorGUI.BeginChangeCheck();
            _placementRulesFoldout = EditorGUILayout.Foldout(_placementRulesFoldout, "정렬 및 배치 규칙", true);
            if (EditorGUI.EndChangeCheck())
                SaveFoldoutPrefs();

            if (!_placementRulesFoldout)
                return;

            EditorGUI.indentLevel++;
            _raycastMask = LayerMaskField("Raycast Layer", _raycastMask);
            _heightOffset = EditorGUILayout.FloatField("Y Offset", _heightOffset);

            _alignToSurface = EditorGUILayout.Toggle("Align To Surface", _alignToSurface);
            _yawOffset = EditorGUILayout.Slider("Yaw Offset", _yawOffset, -180f, 180f);

            _snapToGrid = EditorGUILayout.Toggle("Snap To Grid", _snapToGrid);
            using (new EditorGUI.DisabledScope(!_snapToGrid))
                _gridSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Grid Size", _gridSize));

            _randomRotation = EditorGUILayout.Toggle("Random Yaw", _randomRotation);
            EditorGUI.indentLevel--;

            DrawBrushSettings();
        }

        private void DrawActorSourceSettings()
        {
            EditorGUILayout.Space(6f);
            EditorGUI.BeginChangeCheck();
            _actorSourceFoldout = EditorGUILayout.Foldout(_actorSourceFoldout, "액터 배치 소스 설정", true);
            if (EditorGUI.EndChangeCheck())
                SaveFoldoutPrefs();

            if (!_actorSourceFoldout)
                return;

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            _actorSource = (ActorPlacementSource)EditorGUILayout.EnumPopup("Source", _actorSource);
            if (EditorGUI.EndChangeCheck())
            {
                _placementMode = false;
                SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
                SceneView.RepaintAll();
            }

            if (_actorSource == ActorPlacementSource.ActorDatabase)
            {
                EditorGUILayout.BeginHorizontal();
                _actorDatabase = (ActorDatabase)EditorGUILayout.ObjectField("ActorDatabase", _actorDatabase, typeof(ActorDatabase), false);
                if (GUILayout.Button("자동", GUILayout.Width(44f)))
                {
                    TryAutoLoadActorDatabase();
                    RefreshActorDefinitions();
                }
                EditorGUILayout.EndHorizontal();

                _actorFilter = (ActorType)EditorGUILayout.EnumFlagsField("Actor Filter", _actorFilter);
            }
            else
            {
                _directActorPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", _directActorPrefab, typeof(GameObject), false);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Project 선택 사용"))
                    UseSelectedProjectPrefab();

                if (GUILayout.Button("Portal 폴더 열기"))
                {
                    var portalFolder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/03.Prefabs/Actor/Portal");
                    if (portalFolder != null)
                        EditorGUIUtility.PingObject(portalFolder);
                }
                EditorGUILayout.EndHorizontal();

                if (_directActorPrefab == null)
                    EditorGUILayout.HelpBox("포탈, 트리거, 장식물처럼 ActorDatabase에 없는 배치물은 직접 프리팹을 연결하세요.", MessageType.Info);
            }

            EditorGUI.indentLevel--;
        }

    }
}
#endif
