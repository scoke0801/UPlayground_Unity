#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Interaction.Enum;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Component;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>
    /// 씬 뷰 클릭으로 채집/벌목/채광/낚시터용 GatheringActor를 배치하는 전용 에디터 도구.
    /// 메뉴: UPlayGround/월드/맵/Gathering 배치 도구
    /// </summary>
    public class GatheringPlacementEditorWindow : EditorWindow
    {
        private const string DefaultRootName = "GatheringPlacementRoot";
        private const string InteractionDataFolder = "Assets/10.Datas/Actor/Interaction";
        private const string SearchControlName = "GatheringPlacement.Search";

        private const string PrefsPrefix = "UPlayground.GatheringPlacement.";
        private const string RecentPrefsKey = PrefsPrefix + "RecentGuids";
        private const string AttachFoldoutPrefsKey = PrefsPrefix + "AttachFoldout";
        private const string PlacementFoldoutPrefsKey = PrefsPrefix + "PlacementFoldout";
        private const char PrefsSeparator = '|';
        private const int MaxRecentCount = 5;

        private readonly List<InteractableActorSO> _interactableDatas = new();
        private readonly List<string> _recentDataGuids = new();

        private InteractableActorSO _selectedData;
        private GameObject _prefab;
        private Transform _parent;
        private string _searchFilter = "";
        private Vector2 _dataListScroll;
        private Vector2 _mainScroll;

        private bool _placementMode;
        private bool _autoCreateRoot = true;
        private bool _selectAfterPlace = true;
        private bool _alignToSurface;
        private bool _snapToGrid;
        private bool _randomYaw;
        private bool _randomRotation;
        private bool _addSceneEntityId = true;
        private bool _autoSetupCollider = true;
        private bool _attachOptionsFoldout = true;
        private bool _placementRulesFoldout = true;
        private float _gridSize = 1f;
        private float _yawOffset;
        private Vector2 _randomRotationXRange = Vector2.zero;
        private Vector2 _randomRotationYRange = new(0f, 360f);
        private Vector2 _randomRotationZRange = Vector2.zero;
        private float _heightOffset;
        private LayerMask _raycastMask = ~0;

        private Vector3 _previewPosition;
        private Vector3 _previewNormal = Vector3.up;
        private bool _hasPreviewHit;

        private int _sessionPlacementCount;
        private string _statusMessage = "배치할 상호작용 데이터를 선택하세요.";
        private MessageType _statusType = MessageType.Info;
        private double _statusMessageExpiresAt;

        private GUIStyle _sectionStyle;
        private GUIStyle _selectedItemStyle;
        private GUIStyle _normalItemStyle;
        private GUIStyle _statusTextStyle;
        private GUIStyle _chipStyle;
        private bool _stylesInitialized;

        [MenuItem("UPlayGround/월드/맵/Gathering 배치 도구", priority = UPlaygroundMenuPriority.WorldMap + 1)]
        public static void Open()
        {
            var window = GetWindow<GatheringPlacementEditorWindow>();
            window.titleContent = new GUIContent("Gathering Placement", EditorGUIUtility.IconContent("d_TerrainInspector.TerrainToolPlants").image);
            window.minSize = new Vector2(420f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            LoadPrefs();
            RefreshInteractableDatas();
            SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            SaveFoldoutPrefs();
        }

        private void OnGUI()
        {
            InitStyles();
            HandleWindowShortcuts();

            DrawStatusBar();
            DrawToolbar();

            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
            DrawDataSection();
            DrawTargetSection();
            DrawPlacementSettings();
            DrawValidationSection();
            EditorGUILayout.EndScrollView();
        }

        private void DrawStatusBar()
        {
            bool canPlace = CanPlace(out _);
            Color barColor = _placementMode
                ? canPlace ? new Color(0.22f, 0.48f, 0.28f) : new Color(0.52f, 0.32f, 0.14f)
                : new Color(0.22f, 0.22f, 0.22f);

            Rect rect = EditorGUILayout.GetControlRect(false, 34f);
            EditorGUI.DrawRect(rect, barColor);

            string modeText = _placementMode ? "배치 모드 ON" : "배치 모드 OFF";
            string selectionText = _selectedData != null
                ? $"{GetDataTitle(_selectedData)} ({_selectedData.interactionObjectType})"
                : "데이터 미선택";

            Rect leftRect = new Rect(rect.x + 10f, rect.y + 5f, rect.width - 170f, 22f);
            GUI.Label(leftRect, $"{modeText} - {selectionText}", _statusTextStyle);

            Rect rightRect = new Rect(rect.xMax - 160f, rect.y + 5f, 150f, 22f);
            GUI.Label(rightRect, $"이번 세션 {_sessionPlacementCount}개", _statusTextStyle);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            using (new EditorGUI.DisabledScope(_selectedData == null))
            {
                if (GUILayout.Button(_placementMode ? "배치 중지" : "배치 시작", EditorStyles.toolbarButton, GUILayout.Width(82f)))
                {
                    _placementMode = !_placementMode;
                    SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button("데이터 새로고침", EditorStyles.toolbarButton, GUILayout.Width(105f)))
                RefreshInteractableDatas();

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(_selectedData == null))
            {
                if (GUILayout.Button("선택 데이터 Ping", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                    EditorGUIUtility.PingObject(_selectedData);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDataSection()
        {
            DrawSectionLabel("데이터 선택");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("검색");
            GUI.SetNextControlName(SearchControlName);
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(24f)))
                _searchFilter = "";
            EditorGUILayout.EndHorizontal();

            DrawRecentDataChips();

            _dataListScroll = EditorGUILayout.BeginScrollView(_dataListScroll, GUILayout.Height(210f));

            bool anyShown = false;
            foreach (var data in _interactableDatas)
            {
                if (!ShouldShowData(data))
                    continue;

                anyShown = true;
                DrawDataRow(data);
            }

            if (!anyShown)
            {
                string emptyMessage = string.IsNullOrWhiteSpace(_searchFilter)
                    ? "등록된 상호작용 데이터가 없습니다. Interaction 데이터 폴더에 SO를 추가하세요."
                    : $"'{_searchFilter}'와(과) 일치하는 데이터가 없습니다.";
                GUILayout.Label(emptyMessage, EditorStyles.centeredGreyMiniLabel, GUILayout.Height(32f));
            }

            EditorGUILayout.EndScrollView();

            if (_selectedData == null)
                EditorGUILayout.HelpBox("배치할 상호작용 데이터를 먼저 선택하세요.", MessageType.Warning);
        }

        private void DrawRecentDataChips()
        {
            PruneRecentDataGuids();
            if (_recentDataGuids.Count == 0)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("최근 사용", EditorStyles.miniBoldLabel, GUILayout.Width(56f));

            for (int i = 0; i < _recentDataGuids.Count; i++)
            {
                var data = LoadDataByGuid(_recentDataGuids[i]);
                if (data == null)
                    continue;

                bool selected = data == _selectedData;
                Color previousColor = GUI.backgroundColor;
                GUI.backgroundColor = selected ? new Color(0.45f, 0.62f, 0.9f) : previousColor;
                if (GUILayout.Button($"{i + 1}. {GetDataTitle(data)}", _chipStyle, GUILayout.MaxWidth(120f)))
                    SelectData(data, false);
                GUI.backgroundColor = previousColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDataRow(InteractableActorSO data)
        {
            bool isSelected = _selectedData == data;
            Rect rect = GUILayoutUtility.GetRect(0f, 32f, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
                (isSelected ? _selectedItemStyle : _normalItemStyle).Draw(rect, GUIContent.none, false, false, isSelected, false);

            if (isSelected && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + 3f, 3f, rect.height - 6f), new Color(0.55f, 0.72f, 1f));

            string title = GetDataTitle(data);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 7f, rect.width * 0.45f, 18f), title, EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + rect.width * 0.45f, rect.y + 8f, rect.width * 0.34f, 16f),
                $"{data.interactionObjectType}  |  HP {data.hp}", EditorStyles.miniLabel);
            GUI.Label(new Rect(rect.xMax - 110f, rect.y + 8f, 104f, 16f), data.name, EditorStyles.miniLabel);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                SelectData(data, false);
                Event.current.Use();
            }
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.Space(8f);
            DrawSectionLabel("배치 대상");

            _prefab = (GameObject)EditorGUILayout.ObjectField("Gathering Prefab", _prefab, typeof(GameObject), false);

            if (_prefab == null)
                DrawInlineNotice("지정된 Prefab이 없어 기본 GameObject + GatheringActor로 생성됩니다.", MessageType.Warning);
            else if (_prefab.GetComponent<GatheringActor>() == null)
                DrawInlineNotice("선택한 프리팹 루트에 GatheringActor가 없습니다. 배치 후 루트에 추가합니다.", MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Project 선택 사용"))
                UseSelectedProjectPrefab();

            if (GUILayout.Button("Interaction 데이터 폴더"))
            {
                var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(InteractionDataFolder);
                if (folder != null)
                    EditorGUIUtility.PingObject(folder);
            }
            EditorGUILayout.EndHorizontal();

            _parent = (Transform)EditorGUILayout.ObjectField("Parent", _parent, typeof(Transform), true);
            _autoCreateRoot = EditorGUILayout.Toggle("Auto Create Root", _autoCreateRoot);
            _selectAfterPlace = EditorGUILayout.Toggle("Select After Place", _selectAfterPlace);
        }

        private void DrawPlacementSettings()
        {
            EditorGUILayout.Space(8f);
            DrawSectionLabel("배치 옵션");

            EditorGUI.BeginChangeCheck();
            _attachOptionsFoldout = EditorGUILayout.Foldout(_attachOptionsFoldout, "부착 옵션", true);
            if (EditorGUI.EndChangeCheck())
                SaveFoldoutPrefs();

            if (_attachOptionsFoldout)
            {
                EditorGUI.indentLevel++;
                _addSceneEntityId = EditorGUILayout.Toggle("Add SceneEntityId", _addSceneEntityId);
                _autoSetupCollider = EditorGUILayout.Toggle("Auto Setup Collider", _autoSetupCollider);
                EditorGUI.indentLevel--;
            }

            EditorGUI.BeginChangeCheck();
            _placementRulesFoldout = EditorGUILayout.Foldout(_placementRulesFoldout, "정렬 및 배치 규칙", true);
            if (EditorGUI.EndChangeCheck())
                SaveFoldoutPrefs();

            if (_placementRulesFoldout)
            {
                EditorGUI.indentLevel++;
                _raycastMask = LayerMaskField("Raycast Layer", _raycastMask);
                _heightOffset = EditorGUILayout.FloatField("Y Offset", _heightOffset);

                _alignToSurface = EditorGUILayout.Toggle("Align To Surface", _alignToSurface);
                _yawOffset = EditorGUILayout.Slider("Yaw Offset", _yawOffset, -180f, 180f);

                _snapToGrid = EditorGUILayout.Toggle("Snap To Grid", _snapToGrid);
                using (new EditorGUI.DisabledScope(!_snapToGrid))
                    _gridSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Grid Size", _gridSize));

                _randomRotation = EditorGUILayout.Toggle("Random Rotation", _randomRotation);
                using (new EditorGUI.DisabledScope(!_randomRotation))
                {
                    EditorGUI.indentLevel++;
                    _randomRotationXRange = DrawAngleRangeField("X Range", _randomRotationXRange);
                    _randomRotationYRange = DrawAngleRangeField("Y Range", _randomRotationYRange);
                    _randomRotationZRange = DrawAngleRangeField("Z Range", _randomRotationZRange);
                    EditorGUI.indentLevel--;
                }

                _randomYaw = false;
                EditorGUI.indentLevel--;
            }
        }

        private void DrawValidationSection()
        {
            EditorGUILayout.Space(8f);
            DrawSectionLabel("검증 및 실행 상태");

            string readiness = BuildReadinessMessage();
            MessageType readinessType = GetReadinessMessageType();
            string status = ShouldShowTemporaryStatus()
                ? _statusMessage
                : readiness;
            MessageType statusType = ShouldShowTemporaryStatus()
                ? _statusType
                : readinessType;

            EditorGUILayout.HelpBox(status, statusType);

            if (_selectedData != null)
            {
                EditorGUILayout.LabelField("선택 데이터", $"{GetDataTitle(_selectedData)} / {_selectedData.interactionObjectType} / HP {_selectedData.hp}");
                EditorGUILayout.LabelField("생성 방식", _prefab != null ? _prefab.name : "기본 GameObject 생성");
            }

            EditorGUILayout.LabelField("단축키", "Esc 종료, Ctrl/Cmd+Z 취소, Ctrl+F 검색, 1~5 최근 사용 전환");
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            Event currentEvent = Event.current;

            if (_placementMode)
            {
                int controlId = GUIUtility.GetControlID(FocusType.Passive);
                HandleUtility.AddDefaultControl(controlId);
            }

            HandleSceneShortcuts(currentEvent);

            if (!_placementMode)
                return;

            UpdatePreview(currentEvent.mousePosition);
            DrawScenePreview();

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                if (!CanPlace(out string reason))
                {
                    SetTemporaryStatus(reason, MessageType.Warning);
                    currentEvent.Use();
                    return;
                }

                PlaceCurrent();
                currentEvent.Use();
            }

            if (currentEvent.type == EventType.MouseMove || currentEvent.type == EventType.MouseDrag)
                sceneView.Repaint();
        }

        private void HandleSceneShortcuts(Event currentEvent)
        {
            if (currentEvent.type != EventType.KeyDown)
                return;

            if (currentEvent.keyCode == KeyCode.Escape && _placementMode)
            {
                _placementMode = false;
                SetPersistentStatus("배치 모드를 종료했습니다.", MessageType.Info);
                currentEvent.Use();
                Repaint();
                SceneView.RepaintAll();
                return;
            }

            if (TrySelectRecentByKey(currentEvent.keyCode))
            {
                currentEvent.Use();
                Repaint();
                SceneView.RepaintAll();
            }
        }

        private void UpdatePreview(Vector2 guiMousePosition)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(guiMousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 10000f, _raycastMask))
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
            if (_snapToGrid && !Event.current.shift)
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

            bool canPlace = CanPlace(out _);
            Handles.color = canPlace ? new Color(0.25f, 0.9f, 0.35f, 0.95f) : Color.red;
            Handles.DrawWireDisc(_previewPosition, _previewNormal, 0.75f);
            Handles.DrawLine(_previewPosition, _previewPosition + _previewNormal.normalized * 1.5f);

            string label = canPlace ? GetDataTitle(_selectedData) : "배치할 데이터 없음";
            Handles.Label(_previewPosition + Vector3.up * 1.25f, label);
        }

        private void PlaceCurrent()
        {
            if (!CanPlace(out string reason))
            {
                SetTemporaryStatus(reason, MessageType.Warning);
                return;
            }

            Transform parent = ResolveParent();
            Quaternion rotation = BuildPlacementRotation();
            GameObject instance = CreateInstance();

            if (instance == null)
                return;

            Undo.RegisterCreatedObjectUndo(instance, "Gathering Placement");
            instance.transform.SetPositionAndRotation(_previewPosition, rotation);

            if (parent != null)
                Undo.SetTransformParent(instance.transform, parent, "Gathering Placement Parent");

            ApplyInteractableLayer(instance);
            SetupColliderIfNeeded(instance);
            ApplyGatheringData(instance);
            AddSceneEntityIdIfNeeded(instance);

            if (_selectAfterPlace)
                Selection.activeGameObject = instance;

            _sessionPlacementCount++;
            AddRecentData(_selectedData);
            SetTemporaryStatus($"배치 완료 (이번 세션 {_sessionPlacementCount}개)", MessageType.Info);
            EditorSceneManager.MarkSceneDirty(instance.scene);
            Repaint();
        }

        private GameObject CreateInstance()
        {
            if (_prefab != null)
            {
                var prefabInstance = PrefabUtility.InstantiatePrefab(_prefab, SceneManager.GetActiveScene()) as GameObject;
                return prefabInstance != null ? prefabInstance : Instantiate(_prefab);
            }

            string objectName = BuildObjectName(_selectedData);
            var instance = new GameObject(objectName);
            instance.AddComponent<GatheringActor>();

            return instance;
        }

        private void ApplyInteractableLayer(GameObject instance)
        {
            int layer = LayerMask.NameToLayer("InteractableObject");
            if (layer < 0)
            {
                Debug.LogWarning("[GatheringPlacement] 'InteractableObject' Layer를 찾지 못했습니다. 배치 오브젝트 Layer를 변경하지 않았습니다.", instance);
                return;
            }

            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                Undo.RecordObject(child.gameObject, "Set Gathering Layer");
                child.gameObject.layer = layer;
                EditorUtility.SetDirty(child.gameObject);
            }
        }

        private void SetupColliderIfNeeded(GameObject instance)
        {
            if (!_autoSetupCollider)
                return;

            if (HasRegularCollider(instance))
                return;

            RemoveMeshColliders(instance);
            AddColliderFromRendererBounds(instance);
        }

        private static bool HasRegularCollider(GameObject instance)
        {
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null && collider is not MeshCollider)
                    return true;
            }

            return false;
        }

        private static void RemoveMeshColliders(GameObject instance)
        {
            foreach (var meshCollider in instance.GetComponentsInChildren<MeshCollider>(true))
            {
                if (meshCollider == null)
                    continue;

                Undo.DestroyObjectImmediate(meshCollider);
            }
        }

        private static void AddColliderFromRendererBounds(GameObject instance)
        {
            if (!TryGetRendererBounds(instance, out Bounds worldBounds))
            {
                var fallbackCollider = Undo.AddComponent<BoxCollider>(instance);
                fallbackCollider.center = Vector3.up * 0.5f;
                fallbackCollider.size = Vector3.one;
                EditorUtility.SetDirty(fallbackCollider);
                return;
            }

            Vector3 localCenter = instance.transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = WorldSizeToLocalSize(instance.transform, worldBounds.size);

            if (ShouldUseCapsule(localSize))
                AddCapsuleCollider(instance, localCenter, localSize);
            else
                AddBoxCollider(instance, localCenter, localSize);
        }

        private static bool TryGetRendererBounds(GameObject instance, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private static Vector3 WorldSizeToLocalSize(Transform root, Vector3 worldSize)
        {
            Vector3 scale = root.lossyScale;
            return new Vector3(
                DivideByScale(worldSize.x, scale.x),
                DivideByScale(worldSize.y, scale.y),
                DivideByScale(worldSize.z, scale.z));
        }

        private static float DivideByScale(float value, float scale)
        {
            scale = Mathf.Abs(scale);
            return scale <= 0.0001f ? value : Mathf.Max(0.01f, value / scale);
        }

        private static bool ShouldUseCapsule(Vector3 size)
        {
            float largest = Mathf.Max(size.x, size.y, size.z);
            float secondLargest = size.x + size.y + size.z - largest - Mathf.Min(size.x, size.y, size.z);
            return largest >= secondLargest * 1.35f;
        }

        private static void AddBoxCollider(GameObject instance, Vector3 center, Vector3 size)
        {
            var collider = Undo.AddComponent<BoxCollider>(instance);
            collider.center = center;
            collider.size = new Vector3(
                Mathf.Max(0.01f, size.x),
                Mathf.Max(0.01f, size.y),
                Mathf.Max(0.01f, size.z));
            EditorUtility.SetDirty(collider);
        }

        private static void AddCapsuleCollider(GameObject instance, Vector3 center, Vector3 size)
        {
            var collider = Undo.AddComponent<CapsuleCollider>(instance);
            collider.center = center;

            if (size.x >= size.y && size.x >= size.z)
            {
                collider.direction = 0;
                collider.radius = Mathf.Max(0.01f, Mathf.Max(size.y, size.z) * 0.5f);
                collider.height = Mathf.Max(size.x, collider.radius * 2f);
            }
            else if (size.z >= size.x && size.z >= size.y)
            {
                collider.direction = 2;
                collider.radius = Mathf.Max(0.01f, Mathf.Max(size.x, size.y) * 0.5f);
                collider.height = Mathf.Max(size.z, collider.radius * 2f);
            }
            else
            {
                collider.direction = 1;
                collider.radius = Mathf.Max(0.01f, Mathf.Max(size.x, size.z) * 0.5f);
                collider.height = Mathf.Max(size.y, collider.radius * 2f);
            }

            EditorUtility.SetDirty(collider);
        }

        private void ApplyGatheringData(GameObject instance)
        {
            var gatheringActor = instance.GetComponent<GatheringActor>();
            if (gatheringActor == null)
                gatheringActor = Undo.AddComponent<GatheringActor>(instance);

            var serializedActor = new SerializedObject(gatheringActor);
            SerializedProperty dataProperty = serializedActor.FindProperty("_interactableData");
            if (dataProperty == null)
            {
                Debug.LogWarning($"[GatheringPlacement] '{instance.name}'에서 _interactableData 프로퍼티를 찾지 못했습니다.", instance);
                return;
            }

            dataProperty.objectReferenceValue = _selectedData;
            serializedActor.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gatheringActor);
        }

        private void AddSceneEntityIdIfNeeded(GameObject instance)
        {
            if (!_addSceneEntityId)
                return;

            var entityId = instance.GetComponent<SceneEntityId>();
            if (entityId == null)
                entityId = Undo.AddComponent<SceneEntityId>(instance);

            if (!entityId.HasGuid)
            {
                Undo.RecordObject(entityId, "Set SceneEntityId GUID");
                entityId.EditorSetGuid(Guid.NewGuid().ToString("N"));
                EditorUtility.SetDirty(entityId);
            }
        }

        private Quaternion BuildPlacementRotation()
        {
            Quaternion localRotation = _randomRotation
                ? Quaternion.Euler(
                    RandomRange(_randomRotationXRange),
                    RandomRange(_randomRotationYRange),
                    RandomRange(_randomRotationZRange))
                : Quaternion.Euler(0f, _yawOffset, 0f);

            if (!_alignToSurface)
                return localRotation;

            return Quaternion.FromToRotation(Vector3.up, _previewNormal) * localRotation;
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
                Undo.RegisterCreatedObjectUndo(root, "Create Gathering Placement Root");
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            return root.transform;
        }

        private void RefreshInteractableDatas()
        {
            _interactableDatas.Clear();

            foreach (string guid in AssetDatabase.FindAssets("t:InteractableActorSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<InteractableActorSO>(path);
                if (data == null || data.GetType() != typeof(InteractableActorSO))
                    continue;

                if (!IsGatheringPlacementData(data))
                    continue;

                _interactableDatas.Add(data);
            }

            _interactableDatas.Sort((a, b) =>
            {
                int typeCompare = a.interactionObjectType.CompareTo(b.interactionObjectType);
                return typeCompare != 0 ? typeCompare : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
            });

            if (_selectedData == null && _interactableDatas.Count > 0)
                SelectData(_interactableDatas[0], false, armPlacement: false);

            if (_selectedData != null && !_interactableDatas.Contains(_selectedData))
                SelectData(_interactableDatas.Count > 0 ? _interactableDatas[0] : null, false, armPlacement: false);

            PruneRecentDataGuids();
            Repaint();
        }

        /// <summary>
        /// armPlacement: 사용자가 직접 선택했을 때만 배치 모드를 켠다.
        /// 창 열기/새로고침의 자동 선택은 false로 호출해 의도치 않은 씬 클릭 배치를 막는다.
        /// </summary>
        private void SelectData(InteractableActorSO data, bool addToRecent, bool armPlacement = true)
        {
            _selectedData = data;

            if (addToRecent && data != null)
                AddRecentData(data);

            if (armPlacement && data != null && !_placementMode)
                _placementMode = true;

            SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
            Repaint();
            SceneView.RepaintAll();
        }

        private bool CanPlace(out string reason)
        {
            if (_selectedData == null)
            {
                reason = "배치할 상호작용 데이터를 먼저 선택하세요.";
                return false;
            }

            if (!_hasPreviewHit)
            {
                reason = "이 위치에는 배치할 표면이 없습니다. Raycast Layer 설정을 확인하세요.";
                return false;
            }

            reason = null;
            return true;
        }

        private string BuildReadinessMessage()
        {
            if (_selectedData == null)
                return "배치할 상호작용 데이터를 먼저 선택하세요.";

            if (_prefab == null)
                return "지정된 Prefab이 없어 기본 GameObject + GatheringActor로 생성됩니다. 씬 뷰 클릭으로 배치할 수 있습니다.";

            if (_prefab.GetComponent<GatheringActor>() == null)
                return "선택한 프리팹 루트에 GatheringActor가 없습니다. 배치 후 루트에 자동 추가됩니다.";

            return "배치 준비 완료 - 씬 뷰 클릭으로 배치하세요. Esc 종료, Ctrl/Cmd+Z 취소.";
        }

        private MessageType GetReadinessMessageType()
        {
            if (_selectedData == null)
                return MessageType.Warning;

            return _prefab == null || _prefab.GetComponent<GatheringActor>() == null
                ? MessageType.Info
                : MessageType.None;
        }

        private void SetPersistentStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            _statusMessageExpiresAt = 0;
        }

        private void SetTemporaryStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            _statusMessageExpiresAt = EditorApplication.timeSinceStartup + 1.5d;
            Repaint();
        }

        private bool ShouldShowTemporaryStatus()
        {
            return _statusMessageExpiresAt > EditorApplication.timeSinceStartup;
        }

        private void OnUndoRedoPerformed()
        {
            // 이 콜백은 창이 열려 있는 동안 모든 Undo/Redo(무관한 편집 포함)에 발화하므로
            // 배치 카운트 조작이나 상태 메시지 없이 프리뷰 갱신만 수행한다.
            SceneView.RepaintAll();
            Repaint();
        }

        private void AddRecentData(InteractableActorSO data)
        {
            string guid = GetDataGuid(data);
            if (string.IsNullOrEmpty(guid))
                return;

            _recentDataGuids.Remove(guid);
            _recentDataGuids.Insert(0, guid);

            if (_recentDataGuids.Count > MaxRecentCount)
                _recentDataGuids.RemoveRange(MaxRecentCount, _recentDataGuids.Count - MaxRecentCount);

            SaveRecentDataGuids();
        }

        private void PruneRecentDataGuids()
        {
            bool changed = false;
            for (int i = _recentDataGuids.Count - 1; i >= 0; i--)
            {
                if (LoadDataByGuid(_recentDataGuids[i]) != null)
                    continue;

                _recentDataGuids.RemoveAt(i);
                changed = true;
            }

            if (changed)
                SaveRecentDataGuids();
        }

        private bool TrySelectRecentByKey(KeyCode keyCode)
        {
            int index = keyCode switch
            {
                KeyCode.Alpha1 => 0,
                KeyCode.Alpha2 => 1,
                KeyCode.Alpha3 => 2,
                KeyCode.Alpha4 => 3,
                KeyCode.Alpha5 => 4,
                _ => -1
            };

            if (index < 0 || index >= _recentDataGuids.Count)
                return false;

            var data = LoadDataByGuid(_recentDataGuids[index]);
            if (data == null)
                return false;

            SelectData(data, false);
            return true;
        }

        private void LoadPrefs()
        {
            _recentDataGuids.Clear();
            string recent = EditorPrefs.GetString(RecentPrefsKey, "");
            if (!string.IsNullOrWhiteSpace(recent))
                _recentDataGuids.AddRange(recent.Split(PrefsSeparator, StringSplitOptions.RemoveEmptyEntries));

            _attachOptionsFoldout = EditorPrefs.GetBool(AttachFoldoutPrefsKey, true);
            _placementRulesFoldout = EditorPrefs.GetBool(PlacementFoldoutPrefsKey, true);
        }

        private void SaveRecentDataGuids()
        {
            EditorPrefs.SetString(RecentPrefsKey, string.Join(PrefsSeparator.ToString(), _recentDataGuids));
        }

        private void SaveFoldoutPrefs()
        {
            EditorPrefs.SetBool(AttachFoldoutPrefsKey, _attachOptionsFoldout);
            EditorPrefs.SetBool(PlacementFoldoutPrefsKey, _placementRulesFoldout);
        }

        private static bool IsGatheringPlacementData(InteractableActorSO data)
        {
            return data.interactionObjectType == InteractionObjectType.TREE
                || data.interactionObjectType == InteractionObjectType.STONE
                || data.interactionObjectType == InteractionObjectType.FISHING_ZONE
                || data.interactionObjectType == InteractionObjectType.GATERING_ZONE;
        }

        private bool ShouldShowData(InteractableActorSO data)
        {
            if (data == null)
                return false;

            if (string.IsNullOrEmpty(_searchFilter))
                return true;

            return ContainsIgnoreCase(data.name, _searchFilter)
                || ContainsIgnoreCase(data.actorName, _searchFilter)
                || ContainsIgnoreCase(data.description, _searchFilter)
                || ContainsIgnoreCase(data.interactionObjectType.ToString(), _searchFilter);
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

            _prefab = selected;
            SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
            Repaint();
        }

        private void HandleWindowShortcuts()
        {
            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown)
                return;

            if ((currentEvent.control || currentEvent.command) && currentEvent.keyCode == KeyCode.F)
            {
                GUI.FocusControl(SearchControlName);
                currentEvent.Use();
                return;
            }

            // 검색창/FloatField 등 텍스트 편집 중에는 숫자·Esc 단축키가 입력을 가로채지 않도록 한다.
            if (EditorGUIUtility.editingTextField)
                return;

            if (currentEvent.keyCode == KeyCode.Escape && _placementMode)
            {
                _placementMode = false;
                SetPersistentStatus("배치 모드를 종료했습니다.", MessageType.Info);
                currentEvent.Use();
                SceneView.RepaintAll();
                return;
            }

            if (TrySelectRecentByKey(currentEvent.keyCode))
                currentEvent.Use();
        }

        private static string BuildObjectName(InteractableActorSO data)
        {
            string baseName = data == null
                ? "GatheringActor"
                : GetDataTitle(data);

            return $"Gathering_{baseName}";
        }

        private static string GetDataTitle(InteractableActorSO data)
        {
            return data == null
                ? "데이터 없음"
                : string.IsNullOrEmpty(data.actorName) ? data.name : data.actorName;
        }

        private static string GetDataGuid(InteractableActorSO data)
        {
            if (data == null)
                return null;

            string path = AssetDatabase.GetAssetPath(data);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
        }

        private static InteractableActorSO LoadDataByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<InteractableActorSO>(path);
        }

        private static bool ContainsIgnoreCase(string text, string filter)
        {
            return !string.IsNullOrEmpty(text)
                && text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Vector2 DrawAngleRangeField(string label, Vector2 range)
        {
            range = EditorGUILayout.Vector2Field(label, range);

            if (range.x > range.y)
                (range.x, range.y) = (range.y, range.x);

            range.x = Mathf.Clamp(range.x, -360f, 360f);
            range.y = Mathf.Clamp(range.y, -360f, 360f);
            return range;
        }

        private static float RandomRange(Vector2 range)
        {
            if (Mathf.Approximately(range.x, range.y))
                return range.x;

            return UnityEngine.Random.Range(range.x, range.y);
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

        private void DrawInlineNotice(string message, MessageType type)
        {
            GUIContent icon = type switch
            {
                MessageType.Error => EditorGUIUtility.IconContent("console.erroricon.sml"),
                MessageType.Warning => EditorGUIUtility.IconContent("console.warnicon.sml"),
                _ => EditorGUIUtility.IconContent("console.infoicon.sml")
            };

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(icon, GUILayout.Width(20f));
            GUILayout.Label(message, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndHorizontal();
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

            _statusTextStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft,
            };

            _chipStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 2, 2),
            };

            _stylesInitialized = true;
        }
    }
}
#endif
