#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>
    /// 씬 뷰 클릭으로 ActorDatabase 항목 또는 직접 프리팹을 맵에 배치하는 에디터 도구.
    /// 메뉴: UPlayGround/Map/Map Placement Tool
    /// </summary>
    public class MapPlacementEditorWindow : EditorWindow
    {
        private enum PlacementSource
        {
            ActorDatabase,
            DirectPrefab,
        }

        private const string DefaultRootName = "MapPlacementRoot";

        private PlacementSource _source = PlacementSource.ActorDatabase;
        private ActorDatabase _actorDatabase;
        private ActorDefinitionSO _selectedActorDefinition;
        private GameObject _directPrefab;
        private Transform _parent;

        private ActorType _actorFilter = ActorType.Monster | ActorType.NPC;
        private string _searchFilter = "";
        private Vector2 _actorListScroll;
        private Vector2 _mainScroll;

        private bool _placementMode;
        private bool _autoCreateRoot = true;
        private bool _selectAfterPlace = true;
        private bool _alignToSurface;
        private bool _snapToGrid;
        private bool _randomYaw;
        private float _gridSize = 1f;
        private float _yawOffset;
        private float _heightOffset;
        private LayerMask _raycastMask = ~0;

        private Vector3 _previewPosition;
        private Vector3 _previewNormal = Vector3.up;
        private bool _hasPreviewHit;

        private GUIStyle _sectionStyle;
        private GUIStyle _selectedItemStyle;
        private GUIStyle _normalItemStyle;
        private bool _stylesInitialized;

        [MenuItem("UPlayGround/World/Map/Map Placement Tool")]
        public static void Open()
        {
            var window = GetWindow<MapPlacementEditorWindow>();
            window.titleContent = new GUIContent("Map Placement", EditorGUIUtility.IconContent("d_Prefab Icon").image);
            window.minSize = new Vector2(420f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            TryAutoLoadActorDatabase();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            InitStyles();
            DrawToolbar();

            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
            DrawSourceSection();
            DrawPlacementSettings();
            DrawSelectedPreview();
            EditorGUILayout.EndScrollView();

            HandleWindowShortcuts();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = _placementMode ? new Color(0.45f, 0.85f, 0.45f) : Color.white;
            if (GUILayout.Button(_placementMode ? "배치 모드 ON" : "배치 모드 OFF", EditorStyles.toolbarButton, GUILayout.Width(110f)))
            {
                _placementMode = !_placementMode;
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = prevColor;

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(GetCurrentPrefab() == null))
            {
                if (GUILayout.Button("선택 프리팹 Ping", EditorStyles.toolbarButton, GUILayout.Width(105f)))
                    EditorGUIUtility.PingObject(GetCurrentPrefab());
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSourceSection()
        {
            DrawSectionLabel("배치 소스");

            EditorGUI.BeginChangeCheck();
            _source = (PlacementSource)EditorGUILayout.EnumPopup("Source", _source);
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();

            if (_source == PlacementSource.ActorDatabase)
                DrawActorDatabaseSource();
            else
                DrawDirectPrefabSource();
        }

        private void DrawActorDatabaseSource()
        {
            EditorGUILayout.BeginHorizontal();
            _actorDatabase = (ActorDatabase)EditorGUILayout.ObjectField("ActorDatabase", _actorDatabase, typeof(ActorDatabase), false);
            if (GUILayout.Button("자동", GUILayout.Width(44f)))
                TryAutoLoadActorDatabase();
            EditorGUILayout.EndHorizontal();

            _actorFilter = (ActorType)EditorGUILayout.EnumFlagsField("Actor Filter", _actorFilter);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("검색");
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(24f)))
                _searchFilter = "";
            EditorGUILayout.EndHorizontal();

            if (_actorDatabase == null)
            {
                EditorGUILayout.HelpBox("ActorDatabase를 연결해야 몬스터/NPC 목록을 사용할 수 있습니다.", MessageType.Warning);
                return;
            }

            DrawActorDefinitionList();
        }

        private void DrawActorDefinitionList()
        {
            _actorListScroll = EditorGUILayout.BeginScrollView(_actorListScroll, GUILayout.Height(180f));

            bool anyShown = false;
            foreach (var definition in _actorDatabase.All)
            {
                if (!ShouldShowDefinition(definition))
                    continue;

                anyShown = true;
                bool isSelected = _selectedActorDefinition == definition;
                var rect = GUILayoutUtility.GetRect(0f, 38f, GUILayout.ExpandWidth(true));

                if (Event.current.type == EventType.Repaint)
                    (isSelected ? _selectedItemStyle : _normalItemStyle).Draw(rect, GUIContent.none, false, false, isSelected, false);

                string displayName = string.IsNullOrEmpty(definition.displayName) ? definition.actorId : definition.displayName;
                GUI.Label(new Rect(rect.x + 8f, rect.y + 4f, rect.width - 16f, 16f), displayName, EditorStyles.boldLabel);
                GUI.Label(new Rect(rect.x + 8f, rect.y + 20f, rect.width - 16f, 14f),
                    $"{definition.actorId}  |  {definition.actorType}", EditorStyles.miniLabel);

                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    _selectedActorDefinition = definition;
                    _source = PlacementSource.ActorDatabase;
                    Repaint();
                    SceneView.RepaintAll();
                    Event.current.Use();
                }
            }

            if (!anyShown)
                GUILayout.Label("표시할 ActorDefinitionSO가 없습니다.", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.EndScrollView();

            if (_selectedActorDefinition != null && _selectedActorDefinition.prefab == null)
                EditorGUILayout.HelpBox("선택한 ActorDefinitionSO의 prefab이 비어 있어 배치할 수 없습니다.", MessageType.Warning);
        }

        private bool ShouldShowDefinition(ActorDefinitionSO definition)
        {
            if (definition == null)
                return false;

            if (_actorFilter != ActorType.None && (definition.actorType & _actorFilter) == 0)
                return false;

            if (string.IsNullOrEmpty(_searchFilter))
                return true;

            return ContainsIgnoreCase(definition.actorId, _searchFilter)
                || ContainsIgnoreCase(definition.displayName, _searchFilter)
                || ContainsIgnoreCase(definition.name, _searchFilter);
        }

        private void DrawDirectPrefabSource()
        {
            _directPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", _directPrefab, typeof(GameObject), false);

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

            if (_directPrefab == null)
                EditorGUILayout.HelpBox("포탈, 트리거, 장식물처럼 ActorDatabase에 없는 배치물은 직접 프리팹을 연결하세요.", MessageType.Info);
        }

        private void DrawPlacementSettings()
        {
            EditorGUILayout.Space(8f);
            DrawSectionLabel("배치 설정");

            _parent = (Transform)EditorGUILayout.ObjectField("Parent", _parent, typeof(Transform), true);
            _autoCreateRoot = EditorGUILayout.Toggle("Auto Create Root", _autoCreateRoot);
            _selectAfterPlace = EditorGUILayout.Toggle("Select After Place", _selectAfterPlace);

            EditorGUILayout.Space(4f);
            _raycastMask = LayerMaskField("Raycast Layer", _raycastMask);
            _heightOffset = EditorGUILayout.FloatField("Y Offset", _heightOffset);

            _alignToSurface = EditorGUILayout.Toggle("Align To Surface", _alignToSurface);
            _yawOffset = EditorGUILayout.Slider("Yaw Offset", _yawOffset, -180f, 180f);

            _snapToGrid = EditorGUILayout.Toggle("Snap To Grid", _snapToGrid);
            using (new EditorGUI.DisabledScope(!_snapToGrid))
                _gridSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Grid Size", _gridSize));

            _randomYaw = EditorGUILayout.Toggle("Random Yaw", _randomYaw);

            EditorGUILayout.HelpBox(
                "배치 모드 ON 상태에서 씬 뷰를 좌클릭하면 현재 선택 프리팹이 배치됩니다.\n" +
                "ESC로 배치 모드를 끄고, Ctrl/Cmd+Z로 Undo할 수 있습니다.",
                MessageType.None);
        }

        private void DrawSelectedPreview()
        {
            EditorGUILayout.Space(8f);
            DrawSectionLabel("현재 선택");

            var prefab = GetCurrentPrefab();
            if (prefab == null)
            {
                EditorGUILayout.HelpBox("배치할 ActorDefinitionSO 또는 직접 프리팹을 선택하세요.", MessageType.Warning);
                return;
            }

            EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

            if (_source == PlacementSource.ActorDatabase && _selectedActorDefinition != null)
            {
                EditorGUILayout.LabelField("Actor ID", _selectedActorDefinition.actorId);
                EditorGUILayout.LabelField("Actor Type", _selectedActorDefinition.actorType.ToString());
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_placementMode)
                return;

            var e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlId);

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                _placementMode = false;
                e.Use();
                Repaint();
                SceneView.RepaintAll();
                return;
            }

            UpdatePreview(e.mousePosition);
            DrawScenePreview();

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                PlaceCurrentPrefab();
                e.Use();
            }

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
                sceneView.Repaint();
        }

        private void UpdatePreview(Vector2 guiMousePosition)
        {
            var ray = HandleUtility.GUIPointToWorldRay(guiMousePosition);
            if (Physics.Raycast(ray, out var hit, 10000f, _raycastMask))
            {
                _previewPosition = ApplyPositionRules(hit.point, hit.normal);
                _previewNormal = hit.normal;
                _hasPreviewHit = true;
                return;
            }

            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                _previewPosition = ApplyPositionRules(ray.GetPoint(enter), Vector3.up);
                _previewNormal = Vector3.up;
                _hasPreviewHit = true;
                return;
            }

            _hasPreviewHit = false;
        }

        private Vector3 ApplyPositionRules(Vector3 position, Vector3 normal)
        {
            if (_snapToGrid)
            {
                position.x = Mathf.Round(position.x / _gridSize) * _gridSize;
                position.z = Mathf.Round(position.z / _gridSize) * _gridSize;
            }

            return position + normal.normalized * _heightOffset;
        }

        private void DrawScenePreview()
        {
            if (!_hasPreviewHit)
                return;

            var prefab = GetCurrentPrefab();
            Handles.color = prefab == null ? Color.red : new Color(0.2f, 0.8f, 1f, 0.95f);
            Handles.DrawWireDisc(_previewPosition, _previewNormal, 0.75f);
            Handles.DrawLine(_previewPosition, _previewPosition + _previewNormal.normalized * 1.5f);

            string label = prefab != null ? prefab.name : "배치 프리팹 없음";
            Handles.Label(_previewPosition + Vector3.up * 1.25f, label);
        }

        private void PlaceCurrentPrefab()
        {
            var prefab = GetCurrentPrefab();
            if (prefab == null || !_hasPreviewHit)
                return;

            Transform parent = ResolveParent();
            Quaternion rotation = BuildPlacementRotation();

            var instance = PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene()) as GameObject;
            if (instance == null)
                instance = Instantiate(prefab);

            Undo.RegisterCreatedObjectUndo(instance, "Map Placement");
            instance.transform.SetPositionAndRotation(_previewPosition, rotation);

            if (parent != null)
                Undo.SetTransformParent(instance.transform, parent, "Map Placement Parent");

            ApplyActorDefinitionIfNeeded(instance);

            if (_selectAfterPlace)
                Selection.activeGameObject = instance;

            EditorSceneManager.MarkSceneDirty(instance.scene);
        }

        private Quaternion BuildPlacementRotation()
        {
            float yaw = _randomYaw ? UnityEngine.Random.Range(0f, 360f) : _yawOffset;
            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);

            if (!_alignToSurface)
                return yawRotation;

            return Quaternion.FromToRotation(Vector3.up, _previewNormal) * yawRotation;
        }

        private Transform ResolveParent()
        {
            if (_parent != null)
                return _parent;

            if (!_autoCreateRoot)
                return null;

            var root = GameObject.Find(DefaultRootName);
            if (root == null)
            {
                root = new GameObject(DefaultRootName);
                Undo.RegisterCreatedObjectUndo(root, "Create Map Placement Root");
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            return root.transform;
        }

        private void ApplyActorDefinitionIfNeeded(GameObject instance)
        {
            if (_source != PlacementSource.ActorDatabase || _selectedActorDefinition == null)
                return;

            var actor = instance.GetComponent<GameActor>();
            if (actor == null)
            {
                Debug.LogWarning($"[MapPlacement] '{instance.name}'에 GameActor 컴포넌트가 없어 actorId를 주입하지 못했습니다.", instance);
                return;
            }

            var serializedActor = new SerializedObject(actor);
            var actorIdProperty = serializedActor.FindProperty("_actorId");
            if (actorIdProperty == null)
            {
                Debug.LogWarning($"[MapPlacement] '{instance.name}'에서 _actorId 프로퍼티를 찾지 못했습니다.", instance);
                return;
            }

            actorIdProperty.stringValue = _selectedActorDefinition.actorId;
            serializedActor.ApplyModifiedPropertiesWithoutUndo();

            if (actor is NpcActor && _selectedActorDefinition.npcData != null)
            {
                var npcDataProperty = serializedActor.FindProperty("_data");
                if (npcDataProperty != null)
                {
                    npcDataProperty.objectReferenceValue = _selectedActorDefinition.npcData;
                    serializedActor.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    Debug.LogWarning($"[MapPlacement] '{instance.name}'에서 NPC _data 프로퍼티를 찾지 못했습니다.", instance);
                }
            }

            EditorUtility.SetDirty(actor);
        }

        private GameObject GetCurrentPrefab()
        {
            return _source == PlacementSource.ActorDatabase
                ? _selectedActorDefinition != null ? _selectedActorDefinition.prefab : null
                : _directPrefab;
        }

        private void TryAutoLoadActorDatabase()
        {
            if (_actorDatabase != null)
                return;

            var guids = AssetDatabase.FindAssets("t:ActorDatabase");
            if (guids.Length == 0)
                return;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _actorDatabase = AssetDatabase.LoadAssetAtPath<ActorDatabase>(path);
        }

        private void UseSelectedProjectPrefab()
        {
            var selected = Selection.activeObject as GameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("프리팹 선택 필요", "Project 창에서 GameObject 프리팹을 선택하세요.", "확인");
                return;
            }

            string path = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(path) || PrefabUtility.GetPrefabAssetType(selected) == PrefabAssetType.NotAPrefab)
            {
                EditorUtility.DisplayDialog("프리팹 선택 필요", "선택한 오브젝트가 프리팹 에셋이 아닙니다.", "확인");
                return;
            }

            _directPrefab = selected;
            _source = PlacementSource.DirectPrefab;
            Repaint();
        }

        private void HandleWindowShortcuts()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown)
                return;

            if (e.keyCode == KeyCode.Escape && _placementMode)
            {
                _placementMode = false;
                e.Use();
                SceneView.RepaintAll();
            }
        }

        private static bool ContainsIgnoreCase(string text, string filter)
        {
            return !string.IsNullOrEmpty(text)
                && text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static LayerMask LayerMaskField(string label, LayerMask selected)
        {
            string[] layers = InternalEditorUtility.layers;
            var layerNumbers = new List<int>(layers.Length);

            foreach (string layerName in layers)
                layerNumbers.Add(LayerMask.NameToLayer(layerName));

            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & selected.value) != 0)
                    maskWithoutEmpty |= 1 << i;
            }

            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers);

            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) != 0)
                    mask |= 1 << layerNumbers[i];
            }

            selected.value = mask;
            return selected;
        }

        private void DrawSectionLabel(string label)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(label, _sectionStyle);
        }

        private void InitStyles()
        {
            if (_stylesInitialized)
                return;

            _sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.7f, 0.85f, 1f) },
            };

            _normalItemStyle = new GUIStyle("box")
            {
                padding = new RectOffset(6, 6, 4, 4),
            };

            _selectedItemStyle = new GUIStyle("box")
            {
                padding = new RectOffset(6, 6, 4, 4),
            };
            _selectedItemStyle.normal.background = Texture2D.grayTexture;

            _stylesInitialized = true;
        }
    }
}
#endif
