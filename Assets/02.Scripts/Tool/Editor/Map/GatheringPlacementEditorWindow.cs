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
    /// 씬 뷰 클릭으로 채집/벌목/채광/낚시터용 GatheringActor와 수동 획득 DropItemActor를 배치하는 에디터 도구.
    /// 메뉴: UPlayGround/월드/맵/월드 인터랙션 배치 도구
    /// </summary>
    public class GatheringPlacementEditorWindow : EditorWindow
    {
        private const string DefaultRootName = "GatheringPlacementRoot";
        private const string DropItemRootName = "DropItemPlacementRoot";
        private const string InteractionDataFolder = "Assets/10.Datas/Actor/Interaction";
        private const string DropItemInteractionDataPath = InteractionDataFolder + "/DropItem.asset";
        private const string ItemDataFolder = "Assets/10.Datas/Item";
        private const string SearchControlName = "WorldInteractionPlacement.Search";
        private const string InteractableObjectLayerName = "InteractableObject";

        private const string PrefsPrefix = "UPlayground.GatheringPlacement.";
        private const string RecentPrefsKey = PrefsPrefix + "RecentGuids";
        private const string AttachFoldoutPrefsKey = PrefsPrefix + "AttachFoldout";
        private const string PlacementFoldoutPrefsKey = PrefsPrefix + "PlacementFoldout";
        private const char PrefsSeparator = '|';
        private const int MaxRecentCount = 5;

        private readonly List<InteractableActorSO> _interactableDatas = new();
        private readonly List<ItemSO> _itemDatas = new();
        private readonly List<string> _recentDataGuids = new();

        private PlacementKind _placementKind = PlacementKind.Gathering;
        private InteractableActorSO _selectedData;
        private ItemSO _selectedItem;
        private GameObject _prefab;
        private GameObject _dropItemPrefab;
        private Transform _parent;
        private string _searchFilter = "";
        private Vector2 _dataListScroll;
        private Vector2 _mainScroll;

        private bool _placementMode;
        private bool _autoCreateRoot = true;
        private bool _selectAfterPlace = true;
        private SurfaceSnapMode _surfaceSnapMode = SurfaceSnapMode.LowerOnly;
        private bool _alignToSurface;
        private bool _snapToGrid;
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
        // LayerMask.NameToLayer는 생성자/필드 초기화식에서 호출이 금지되므로 OnEnable에서 초기화한다.
        private LayerMask _raycastMask = ~0;
        // 낚시터처럼 트리거 수면 위에 배치하는 경우가 있으므로 기본값은 트리거 허용(전역 설정 따름).
        private bool _ignoreTriggerColliders;

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

        private int _dropItemCount = 1;

        private enum PlacementKind
        {
            Gathering = 0,
            DropItem = 1,
        }

        /// <summary>표면 스냅 방식. 밑면 피벗 프리팹은 LowerOnly, 중앙 피벗 프리팹(바위 등)은 Full 사용.</summary>
        private enum SurfaceSnapMode
        {
            None = 0,
            LowerOnly = 1,
            Full = 2,
        }

        private readonly struct PlacementInstance
        {
            public readonly GameObject Root;
            public readonly GameObject SurfaceTarget;
            public readonly bool MoveSurfaceTargetOnly;

            public PlacementInstance(GameObject root, GameObject surfaceTarget, bool moveSurfaceTargetOnly)
            {
                Root = root;
                SurfaceTarget = surfaceTarget;
                MoveSurfaceTargetOnly = moveSurfaceTargetOnly;
            }
        }

        [MenuItem("UPlayGround/월드/맵/월드 인터랙션 배치 도구", priority = UPlaygroundMenuPriority.WorldMap + 1)]
        public static void Open()
        {
            var window = GetWindow<GatheringPlacementEditorWindow>();
            window.titleContent = new GUIContent("World Placement", EditorGUIUtility.IconContent("d_TerrainInspector.TerrainToolPlants").image);
            window.minSize = new Vector2(420f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            _raycastMask = CreateDefaultRaycastMask();
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            LoadPrefs();
            RefreshInteractableDatas();
            RefreshItemDatas();
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
            DrawPlacementKindTabs();
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
            string selectionText = GetSelectionStatusText();

            Rect leftRect = new Rect(rect.x + 10f, rect.y + 5f, rect.width - 170f, 22f);
            GUI.Label(leftRect, $"{modeText} - {selectionText}", _statusTextStyle);

            Rect rightRect = new Rect(rect.xMax - 160f, rect.y + 5f, 150f, 22f);
            GUI.Label(rightRect, $"이번 세션 {_sessionPlacementCount}개", _statusTextStyle);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            using (new EditorGUI.DisabledScope(!HasSelectedPlacementData()))
            {
                if (GUILayout.Button(_placementMode ? "배치 중지" : "배치 시작", EditorStyles.toolbarButton, GUILayout.Width(82f)))
                {
                    _placementMode = !_placementMode;
                    SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button("데이터 새로고침", EditorStyles.toolbarButton, GUILayout.Width(105f)))
            {
                RefreshInteractableDatas();
                RefreshItemDatas();
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(GetSelectedPingObject() == null))
            {
                if (GUILayout.Button("선택 데이터 Ping", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                    EditorGUIUtility.PingObject(GetSelectedPingObject());
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPlacementKindTabs()
        {
            EditorGUILayout.Space(6f);
            EditorGUI.BeginChangeCheck();
            _placementKind = (PlacementKind)GUILayout.Toolbar((int)_placementKind, new[] { "Gathering", "Drop Item" });
            if (EditorGUI.EndChangeCheck())
            {
                _placementMode = false;
                SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
                SceneView.RepaintAll();
            }
        }

        private void DrawDataSection()
        {
            DrawSectionLabel(_placementKind == PlacementKind.Gathering ? "상호작용 데이터 선택" : "드랍 아이템 선택");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("검색");
            GUI.SetNextControlName(SearchControlName);
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(24f)))
                _searchFilter = "";
            EditorGUILayout.EndHorizontal();

            if (_placementKind == PlacementKind.Gathering)
                DrawRecentDataChips();

            _dataListScroll = EditorGUILayout.BeginScrollView(_dataListScroll, GUILayout.Height(210f));

            bool anyShown = false;
            if (_placementKind == PlacementKind.Gathering)
            {
                foreach (var data in _interactableDatas)
                {
                    if (!ShouldShowData(data))
                        continue;

                    anyShown = true;
                    DrawDataRow(data);
                }
            }
            else
            {
                foreach (var item in _itemDatas)
                {
                    if (!ShouldShowItem(item))
                        continue;

                    anyShown = true;
                    DrawItemRow(item);
                }
            }

            if (!anyShown)
            {
                string emptyMessage = string.IsNullOrWhiteSpace(_searchFilter)
                    ? GetEmptyDataMessage()
                    : $"'{_searchFilter}'와(과) 일치하는 데이터가 없습니다.";
                GUILayout.Label(emptyMessage, EditorStyles.centeredGreyMiniLabel, GUILayout.Height(32f));
            }

            EditorGUILayout.EndScrollView();

            if (!HasSelectedPlacementData())
                EditorGUILayout.HelpBox(GetSelectionRequiredMessage(), MessageType.Warning);
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

        private void DrawItemRow(ItemSO item)
        {
            bool isSelected = _selectedItem == item;
            Rect rect = GUILayoutUtility.GetRect(0f, 34f, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
                (isSelected ? _selectedItemStyle : _normalItemStyle).Draw(rect, GUIContent.none, false, false, isSelected, false);

            if (isSelected && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + 3f, 3f, rect.height - 6f), new Color(0.55f, 0.72f, 1f));

            string title = GetItemTitle(item);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 5f, rect.width * 0.42f, 18f), title, EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + rect.width * 0.42f, rect.y + 6f, rect.width * 0.28f, 16f),
                $"ID {item.itemId}  |  {item.itemType}", EditorStyles.miniLabel);
            GUI.Label(new Rect(rect.xMax - 110f, rect.y + 6f, 104f, 16f), item.name, EditorStyles.miniLabel);

            if (item.icon != null)
            {
                Rect iconRect = new Rect(rect.xMax - 136f, rect.y + 4f, 24f, 24f);
                GUI.DrawTexture(iconRect, item.icon.texture, ScaleMode.ScaleToFit, true);
            }

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                SelectItem(item, false);
                Event.current.Use();
            }
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.Space(8f);
            DrawSectionLabel("배치 대상");

            if (_placementKind == PlacementKind.Gathering)
            {
                _prefab = (GameObject)EditorGUILayout.ObjectField("Gathering Prefab", _prefab, typeof(GameObject), false);

                if (_prefab == null)
                    DrawInlineNotice("지정된 Prefab이 없어 기본 GameObject + GatheringActor로 생성됩니다.", MessageType.Warning);
                else if (_prefab.GetComponent<GatheringActor>() == null)
                    DrawInlineNotice("선택한 프리팹 루트에 GatheringActor가 없습니다. 배치 후 루트에 추가합니다.", MessageType.Warning);
            }
            else
            {
                _dropItemPrefab = (GameObject)EditorGUILayout.ObjectField("DropItem Prefab", _dropItemPrefab, typeof(GameObject), false);
                _dropItemCount = Mathf.Max(1, EditorGUILayout.IntField("Item Count", _dropItemCount));

                if (_dropItemPrefab == null)
                    DrawInlineNotice("지정된 Prefab이 없어 기본 GameObject + DropItemActor로 생성됩니다.", MessageType.Warning);
                else if (_dropItemPrefab.GetComponent<DropItemActor>() == null)
                    DrawInlineNotice("선택한 프리팹 루트에 DropItemActor가 없습니다. 배치 후 루트에 추가합니다.", MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Project 선택 사용"))
                UseSelectedProjectPrefab();

            if (GUILayout.Button(_placementKind == PlacementKind.Gathering ? "Interaction 데이터 폴더" : "Item 데이터 폴더"))
            {
                string folderPath = _placementKind == PlacementKind.Gathering ? InteractionDataFolder : ItemDataFolder;
                var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
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
                _ignoreTriggerColliders = EditorGUILayout.Toggle(
                    new GUIContent("Ignore Triggers", "배치 레이캐스트가 트리거 콜라이더를 무시합니다. 트리거 수면 위에 낚시터를 배치할 때는 꺼두세요."),
                    _ignoreTriggerColliders);
                _surfaceSnapMode = (SurfaceSnapMode)EditorGUILayout.Popup(
                    new GUIContent("Surface Snap", "내리기만: 떠 있을 때만 표면까지 내림(밑면 피벗 프리팹 권장). 양방향: 최저점을 표면에 맞춰 올리기도 함(중앙 피벗 바위 등)."),
                    (int)_surfaceSnapMode,
                    new[]
                    {
                        new GUIContent("없음"),
                        new GUIContent("내리기만 (기본)"),
                        new GUIContent("양방향 (최저점 스냅)"),
                    });
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

            if (_placementKind == PlacementKind.Gathering && _selectedData != null)
            {
                EditorGUILayout.LabelField("선택 데이터", $"{GetDataTitle(_selectedData)} / {_selectedData.interactionObjectType} / HP {_selectedData.hp}");
                EditorGUILayout.LabelField("생성 방식", _prefab != null ? _prefab.name : "기본 GameObject 생성");
            }
            else if (_placementKind == PlacementKind.DropItem && _selectedItem != null)
            {
                EditorGUILayout.LabelField("선택 아이템", $"{GetItemTitle(_selectedItem)} / ID {_selectedItem.itemId} / {_selectedItem.itemType} x{_dropItemCount}");
                EditorGUILayout.LabelField("생성 방식", _dropItemPrefab != null ? _dropItemPrefab.name : "기본 GameObject 생성");
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
            if (Physics.Raycast(ray, out RaycastHit hit, 10000f, _raycastMask, GetTriggerInteraction()))
            {
                _previewPosition = ApplyPositionRules(hit.point, hit.normal, out _previewNormal);
                _hasPreviewHit = true;
                return;
            }

            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                _previewPosition = ApplyPositionRules(ray.GetPoint(enter), Vector3.up, out _previewNormal);
                _hasPreviewHit = true;
                return;
            }

            _hasPreviewHit = false;
        }

        private Vector3 ApplyPositionRules(Vector3 position, Vector3 normal, out Vector3 resolvedNormal)
        {
            resolvedNormal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;

            if (_snapToGrid && Event.current != null && !Event.current.shift)
            {
                position.x = Mathf.Round(position.x / _gridSize) * _gridSize;
                position.z = Mathf.Round(position.z / _gridSize) * _gridSize;
                position = ResolveSurfaceAtPosition(position, resolvedNormal, out resolvedNormal);
            }

            return position + resolvedNormal * _heightOffset;
        }

        private Vector3 ResolveSurfaceAtPosition(Vector3 position, Vector3 fallbackNormal, out Vector3 resolvedNormal)
        {
            const float verticalProbeHalfHeight = 5000f;
            var origin = new Vector3(position.x, position.y + verticalProbeHalfHeight, position.z);
            var ray = new Ray(origin, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, verticalProbeHalfHeight * 2f, _raycastMask, GetTriggerInteraction()))
            {
                resolvedNormal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;
                return hit.point;
            }

            resolvedNormal = fallbackNormal.sqrMagnitude > 0.0001f ? fallbackNormal.normalized : Vector3.up;
            return position;
        }

        private QueryTriggerInteraction GetTriggerInteraction()
        {
            return _ignoreTriggerColliders ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.UseGlobal;
        }

        private void DrawScenePreview()
        {
            if (!_hasPreviewHit)
                return;

            bool canPlace = CanPlace(out _);
            Handles.color = canPlace ? new Color(0.25f, 0.9f, 0.35f, 0.95f) : Color.red;
            Handles.DrawWireDisc(_previewPosition, _previewNormal, 0.75f);
            Handles.DrawLine(_previewPosition, _previewPosition + _previewNormal.normalized * 1.5f);

            string label = canPlace ? GetSelectedPlacementTitle() : "배치할 데이터 없음";
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
            PlacementInstance placement = CreateInstance();
            GameObject instance = placement.Root;

            if (instance == null)
                return;

            Undo.RegisterCreatedObjectUndo(instance, "Gathering Placement");
            if (parent != null)
                Undo.SetTransformParent(instance.transform, parent, "Gathering Placement Parent");

            // Parent가 위치/회전/스케일을 가진 경우를 대비해 Parent 연결 이후 월드 배치 좌표를 확정한다.
            instance.transform.SetPositionAndRotation(_previewPosition, rotation);

            ApplyInteractableLayer(instance);
            StickInstanceToSurface(placement);
            SetupColliderIfNeeded(instance);
            ApplyPlacementData(instance);
            AddSceneEntityIdIfNeeded(instance);

            if (_selectAfterPlace)
                Selection.activeGameObject = instance;

            _sessionPlacementCount++;
            if (_placementKind == PlacementKind.Gathering)
                AddRecentData(_selectedData);
            SetTemporaryStatus($"배치 완료 (이번 세션 {_sessionPlacementCount}개)", MessageType.Info);
            EditorSceneManager.MarkSceneDirty(instance.scene);
            Repaint();
        }

        private PlacementInstance CreateInstance()
        {
            GameObject targetPrefab = _placementKind == PlacementKind.Gathering ? _prefab : _dropItemPrefab;
            if (targetPrefab != null)
            {
                Type actorType = _placementKind == PlacementKind.Gathering
                    ? typeof(GatheringActor)
                    : typeof(DropItemActor);

                if (targetPrefab.GetComponent(actorType) != null)
                {
                    var prefabInstance = InstantiatePrefab(targetPrefab);
                    return new PlacementInstance(prefabInstance, prefabInstance, moveSurfaceTargetOnly: false);
                }

                var root = new GameObject(BuildObjectName());
                var visual = InstantiatePrefab(targetPrefab);
                visual.name = targetPrefab.name;
                Undo.RegisterCreatedObjectUndo(visual, "Gathering Placement");
                Undo.SetTransformParent(visual.transform, root.transform, "Attach Placement Visual");
                // 프리팹 에셋 루트에 저장된 위치는 씬 저작 잔여값일 수 있으므로 오프셋으로 복사하지 않는다.
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = targetPrefab.transform.localRotation;
                visual.transform.localScale = targetPrefab.transform.localScale;

                if (_placementKind == PlacementKind.Gathering)
                    root.AddComponent<GatheringActor>();
                else
                    root.AddComponent<DropItemActor>();

                return new PlacementInstance(root, visual, moveSurfaceTargetOnly: true);
            }

            string objectName = BuildObjectName();
            var instance = new GameObject(objectName);
            if (_placementKind == PlacementKind.Gathering)
                instance.AddComponent<GatheringActor>();
            else
                instance.AddComponent<DropItemActor>();

            return new PlacementInstance(instance, instance, moveSurfaceTargetOnly: false);
        }

        private static GameObject InstantiatePrefab(GameObject targetPrefab)
        {
            var prefabInstance = PrefabUtility.InstantiatePrefab(targetPrefab, SceneManager.GetActiveScene()) as GameObject;
            return prefabInstance != null ? prefabInstance : Instantiate(targetPrefab);
        }

        private void ApplyInteractableLayer(GameObject instance)
        {
            int layer = LayerMask.NameToLayer(InteractableObjectLayerName);
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

        private void StickInstanceToSurface(PlacementInstance placement)
        {
            if (_surfaceSnapMode == SurfaceSnapMode.None)
                return;

            GameObject surfaceTarget = placement.SurfaceTarget;
            if (surfaceTarget == null)
                return;

            Vector3 surfaceNormal = _previewNormal.sqrMagnitude > 0.0001f ? _previewNormal.normalized : Vector3.up;
            if (!TryGetLowestSupportProjection(surfaceTarget, surfaceNormal, out float lowestProjection))
                return;

            float targetProjection = Vector3.Dot(_previewPosition, surfaceNormal);
            float offset = targetProjection - lowestProjection;

            // 내리기만 모드: 최저점이 표면 아래에 있으면(뿌리·밑동 등 파묻히라고 만든 여유 지오메트리)
            // 저작된 피벗을 신뢰하고 끌어올리지 않는다. 비주얼이 표면 위에 떠 있을 때만 아래로 내린다.
            if (_surfaceSnapMode == SurfaceSnapMode.LowerOnly && offset >= -0.0001f)
                return;

            if (Mathf.Abs(offset) <= 0.0001f)
                return;

            Transform targetTransform = placement.MoveSurfaceTargetOnly
                ? surfaceTarget.transform
                : placement.Root.transform;
            Undo.RecordObject(targetTransform, "Stick Gathering To Surface");
            targetTransform.position += surfaceNormal * offset;
            EditorUtility.SetDirty(targetTransform);
        }

        private static bool TryGetLowestSupportProjection(GameObject instance, Vector3 normal, out float lowestProjection)
        {
            lowestProjection = float.PositiveInfinity;
            bool hasProjection = false;

            // 비활성 자식(채집 후 그루터기, 파편 등)이나 꺼진 렌더러는 최저점 계산을 오염시키므로 제외한다.
            foreach (var meshFilter in instance.GetComponentsInChildren<MeshFilter>(false))
            {
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                if (!meshFilter.TryGetComponent(out MeshRenderer meshRenderer) || !meshRenderer.enabled)
                    continue;

                EncapsulateLocalBoundsProjection(meshFilter.transform, meshFilter.sharedMesh.bounds, normal, ref lowestProjection);
                hasProjection = true;
            }

            foreach (var skinnedMeshRenderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null || !skinnedMeshRenderer.enabled)
                    continue;

                // SkinnedMeshRenderer.localBounds는 rootBone 기준 공간이다 (rootBone이 없으면 SMR 트랜스폼 기준).
                Transform boundsSpace = skinnedMeshRenderer.rootBone != null
                    ? skinnedMeshRenderer.rootBone
                    : skinnedMeshRenderer.transform;
                EncapsulateLocalBoundsProjection(boundsSpace, skinnedMeshRenderer.localBounds, normal, ref lowestProjection);
                hasProjection = true;
            }

            if (hasProjection)
                return true;

            // 배치 직후에는 물리 동기화 전이라 collider.bounds가 이동 전 위치 기준일 수 있다.
            Physics.SyncTransforms();

            Bounds bounds = default;
            foreach (var collider in instance.GetComponentsInChildren<Collider>(false))
            {
                if (collider == null || !collider.enabled)
                    continue;

                if (!hasProjection)
                {
                    bounds = collider.bounds;
                    hasProjection = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            if (!hasProjection)
                return false;

            lowestProjection = GetLowestWorldAabbProjection(bounds, normal);
            return true;
        }

        private static void EncapsulateLocalBoundsProjection(Transform transform, Bounds localBounds, Vector3 normal, ref float lowestProjection)
        {
            Vector3 min = localBounds.min;
            Vector3 max = localBounds.max;

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        var localCorner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 worldCorner = transform.TransformPoint(localCorner);
                        lowestProjection = Mathf.Min(lowestProjection, Vector3.Dot(worldCorner, normal));
                    }
                }
            }
        }

        private static float GetLowestWorldAabbProjection(Bounds bounds, Vector3 normal)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            float lowest = float.PositiveInfinity;

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        var corner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        lowest = Mathf.Min(lowest, Vector3.Dot(corner, normal));
                    }
                }
            }

            return lowest;
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

        private void ApplyPlacementData(GameObject instance)
        {
            if (_placementKind == PlacementKind.Gathering)
            {
                ApplyGatheringData(instance);
                return;
            }

            ApplyDropItemData(instance);
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

        private void ApplyDropItemData(GameObject instance)
        {
            var dropItemActor = instance.GetComponent<DropItemActor>();
            if (dropItemActor == null)
                dropItemActor = Undo.AddComponent<DropItemActor>(instance);

            var serializedActor = new SerializedObject(dropItemActor);
            SerializedProperty itemProperty = serializedActor.FindProperty("_itemData");
            SerializedProperty countProperty = serializedActor.FindProperty("_count");
            SerializedProperty interactionDataProperty = serializedActor.FindProperty("_interactionData");

            if (itemProperty == null || countProperty == null || interactionDataProperty == null)
            {
                Debug.LogWarning($"[GatheringPlacement] '{instance.name}'에서 DropItemActor 아이템 프로퍼티를 찾지 못했습니다.", instance);
                return;
            }

            itemProperty.objectReferenceValue = _selectedItem;
            countProperty.intValue = Mathf.Max(1, _dropItemCount);
            interactionDataProperty.objectReferenceValue = GetOrCreateDropItemInteractionData();
            serializedActor.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dropItemActor);
        }

        private static InteractableActorSO GetOrCreateDropItemInteractionData()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:InteractableActorSO", new[] { InteractionDataFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<InteractableActorSO>(path);
                if (data != null && data.interactionObjectType == InteractionObjectType.DROP_ITEM)
                    return data;
            }

            EnsureFolder(InteractionDataFolder);

            var dropItemData = ScriptableObject.CreateInstance<InteractableActorSO>();
            dropItemData.actorName = "드랍 아이템";
            dropItemData.description = "맵 배치/드랍 아이템 줍기용 기본 상호작용 데이터";
            dropItemData.interactionObjectType = InteractionObjectType.DROP_ITEM;
            dropItemData.hp = 1;
            dropItemData.showInfoUI = false;
            dropItemData.showShakeEffect = false;
            dropItemData.reviveDowned = false;

            AssetDatabase.CreateAsset(dropItemData, DropItemInteractionDataPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<InteractableActorSO>(DropItemInteractionDataPath);
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

            string rootName = _placementKind == PlacementKind.Gathering ? DefaultRootName : DropItemRootName;
            var root = GameObject.Find(rootName);
            if (root == null)
            {
                root = new GameObject(rootName);
                Undo.RegisterCreatedObjectUndo(root, "Create World Placement Root");
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

        private void RefreshItemDatas()
        {
            _itemDatas.Clear();

            foreach (string guid in AssetDatabase.FindAssets("t:ItemSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
                if (data == null)
                    continue;

                _itemDatas.Add(data);
            }

            _itemDatas.Sort((a, b) =>
            {
                int idCompare = a.itemId.CompareTo(b.itemId);
                return idCompare != 0 ? idCompare : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
            });

            if (_selectedItem == null && _itemDatas.Count > 0)
                SelectItem(_itemDatas[0], false, armPlacement: false);

            if (_selectedItem != null && !_itemDatas.Contains(_selectedItem))
                SelectItem(_itemDatas.Count > 0 ? _itemDatas[0] : null, false, armPlacement: false);

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

        private void SelectItem(ItemSO item, bool addToRecent, bool armPlacement = true)
        {
            _selectedItem = item;

            if (armPlacement && item != null && !_placementMode)
                _placementMode = true;

            SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
            Repaint();
            SceneView.RepaintAll();
        }

        private bool CanPlace(out string reason)
        {
            if (!HasSelectedPlacementData())
            {
                reason = GetSelectionRequiredMessage();
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
            if (!HasSelectedPlacementData())
                return GetSelectionRequiredMessage();

            if (_placementKind == PlacementKind.DropItem)
            {
                if (_dropItemPrefab == null)
                    return "지정된 Prefab이 없어 기본 GameObject + DropItemActor로 생성됩니다. 씬 뷰 클릭으로 배치할 수 있습니다.";

                if (_dropItemPrefab.GetComponent<DropItemActor>() == null)
                    return "선택한 프리팹 루트에 DropItemActor가 없습니다. 배치 후 루트에 자동 추가됩니다.";

                return "DropItem 배치 준비 완료 - 씬 뷰 클릭으로 배치하세요. Esc 종료, Ctrl/Cmd+Z 취소.";
            }

            if (_prefab == null)
                return "지정된 Prefab이 없어 기본 GameObject + GatheringActor로 생성됩니다. 씬 뷰 클릭으로 배치할 수 있습니다.";

            if (_prefab.GetComponent<GatheringActor>() == null)
                return "선택한 프리팹 루트에 GatheringActor가 없습니다. 배치 후 루트에 자동 추가됩니다.";

            return "배치 준비 완료 - 씬 뷰 클릭으로 배치하세요. Esc 종료, Ctrl/Cmd+Z 취소.";
        }

        private MessageType GetReadinessMessageType()
        {
            if (!HasSelectedPlacementData())
                return MessageType.Warning;

            if (_placementKind == PlacementKind.DropItem)
                return _dropItemPrefab == null || _dropItemPrefab.GetComponent<DropItemActor>() == null
                    ? MessageType.Info
                    : MessageType.None;

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
            if (_placementKind != PlacementKind.Gathering)
                return false;

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

        private bool ShouldShowItem(ItemSO item)
        {
            if (item == null)
                return false;

            if (string.IsNullOrEmpty(_searchFilter))
                return true;

            return ContainsIgnoreCase(item.name, _searchFilter)
                || ContainsIgnoreCase(item.itemName, _searchFilter)
                || ContainsIgnoreCase(item.itemDescription, _searchFilter)
                || ContainsIgnoreCase(item.itemType.ToString(), _searchFilter)
                || item.itemId.ToString().IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
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

            if (_placementKind == PlacementKind.Gathering)
                _prefab = selected;
            else
                _dropItemPrefab = selected;

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

        private string BuildObjectName()
        {
            if (_placementKind == PlacementKind.DropItem)
                return $"DropItem_{GetItemTitle(_selectedItem)}";

            string baseName = _selectedData == null
                ? "GatheringActor"
                : GetDataTitle(_selectedData);

            return $"Gathering_{baseName}";
        }

        private bool HasSelectedPlacementData()
        {
            return _placementKind == PlacementKind.Gathering
                ? _selectedData != null
                : _selectedItem != null;
        }

        private string GetSelectionRequiredMessage()
        {
            return _placementKind == PlacementKind.Gathering
                ? "배치할 상호작용 데이터를 먼저 선택하세요."
                : "배치할 아이템 데이터를 먼저 선택하세요.";
        }

        private string GetEmptyDataMessage()
        {
            return _placementKind == PlacementKind.Gathering
                ? "등록된 상호작용 데이터가 없습니다. Interaction 데이터 폴더에 SO를 추가하세요."
                : "등록된 아이템 데이터가 없습니다. Item 데이터 폴더에 SO를 추가하세요.";
        }

        private string GetSelectionStatusText()
        {
            if (_placementKind == PlacementKind.Gathering)
            {
                return _selectedData != null
                    ? $"{GetDataTitle(_selectedData)} ({_selectedData.interactionObjectType})"
                    : "데이터 미선택";
            }

            return _selectedItem != null
                ? $"{GetItemTitle(_selectedItem)} x{_dropItemCount}"
                : "아이템 미선택";
        }

        private string GetSelectedPlacementTitle()
        {
            return _placementKind == PlacementKind.Gathering
                ? GetDataTitle(_selectedData)
                : GetItemTitle(_selectedItem);
        }

        private UnityEngine.Object GetSelectedPingObject()
        {
            return _placementKind == PlacementKind.Gathering
                ? _selectedData
                : _selectedItem;
        }

        private static string GetDataTitle(InteractableActorSO data)
        {
            return data == null
                ? "데이터 없음"
                : string.IsNullOrEmpty(data.actorName) ? data.name : data.actorName;
        }

        private static string GetItemTitle(ItemSO item)
        {
            return item == null
                ? "아이템 없음"
                : string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;
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

        private static LayerMask CreateDefaultRaycastMask()
        {
            int mask = Physics.DefaultRaycastLayers;
            int interactableLayer = LayerMask.NameToLayer(InteractableObjectLayerName);
            if (interactableLayer >= 0)
                mask &= ~(1 << interactableLayer);

            return mask;
        }

        private static void EnsureFolder(string folder)
        {
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
