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
    /// <summary>
    /// 씬 뷰 클릭으로 액터와 상호작용 오브젝트를 배치하는 에디터 도구.
    /// 메뉴: UPlayGround/월드/맵/월드 배치 도구
    /// </summary>
    public partial class GatheringPlacementEditorWindow : EditorWindow
    {
        private const string ActorPlacementRootName = "MapPlacementRoot";
        private const string DefaultRootName = "GatheringPlacementRoot";
        private const string DropItemRootName = "DropItemPlacementRoot";
        private const string InteractionDataFolder = "Assets/10.Datas/Actor/Interaction";
        private const string DropItemInteractionDataPath = InteractionDataFolder + "/DropItem.asset";
        private const string ItemDataFolder = "Assets/10.Datas/Item";
        private const string SearchControlName = "WorldInteractionPlacement.Search";
        private const string InteractableObjectLayerName = "InteractableObject";
        private const float LeftPanelWidth = 300f;

        private const string PrefsPrefix = "UPlayground.GatheringPlacement.";
        private const string RecentPrefsKey = PrefsPrefix + "RecentGuids";
        private const string AttachFoldoutPrefsKey = PrefsPrefix + "AttachFoldout";
        private const string PlacementFoldoutPrefsKey = PrefsPrefix + "PlacementFoldout";
        private const string SourceFoldoutPrefsKey = PrefsPrefix + "SourceFoldout";
        private const string BakeFoldoutPrefsKey = PrefsPrefix + "BakeFoldout";
        private const char PrefsSeparator = '|';
        private const int MaxRecentCount = 5;

        private readonly List<ActorDefinitionSO> _actorDefinitions = new();
        private readonly List<InteractableActorSO> _interactableDatas = new();
        private readonly List<ItemSO> _itemDatas = new();
        private readonly List<string> _recentDataGuids = new();

        private WorldPlacementMode _worldPlacementMode = WorldPlacementMode.Actor;
        private ActorPlacementSource _actorSource = ActorPlacementSource.ActorDatabase;
        private ActorDatabase _actorDatabase;
        private ActorDefinitionSO _selectedActorDefinition;
        private GameObject _directActorPrefab;
        private ActorType _actorFilter = ActorType.Player | ActorType.Monster | ActorType.NPC;
        private string _actorSearchFilter = "";
        private Vector2 _actorListScroll;
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
        private bool _addPlacementMetadata = true;
        private WorldPlacementMetadata.PlacementBakeMode _placementBakeMode = WorldPlacementMetadata.PlacementBakeMode.SceneObject;
        private bool _attachOptionsFoldout = true;
        private bool _placementRulesFoldout = true;
        // 액터 배치 소스는 최초 자동 연결 후 거의 바꾸지 않으므로 기본 접힘.
        private bool _actorSourceFoldout;
        private bool _bakeFoldout = true;
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

        // 몬스터 그룹 지정 — 배치되는 MonsterActor를 이 그룹 하위로 넣어 자동 소속시킨다.
        private MonsterGroupController _targetGroup;

        // Bake 데이터 뷰어
        private readonly List<WorldPlacementDataSO> _bakedDataAssets = new();
        private WorldPlacementDataSO _selectedBakedData;
        private Vector2 _bakedListScroll;
        private bool _showBakedInScene;

        private int _sessionPlacementCount;
        private string _statusMessage = "배치할 상호작용 데이터를 선택하세요.";
        private MessageType _statusType = MessageType.Info;
        private double _statusMessageExpiresAt;

        private GUIStyle _sectionStyle;
        private GUIStyle _selectedItemStyle;
        private GUIStyle _normalItemStyle;
        private GUIStyle _statusTextStyle;
        private GUIStyle _chipStyle;
        private GUIStyle _selectionCaptionStyle;
        private GUIStyle _selectionTitleStyle;
        private GUIStyle _selectionDetailStyle;
        private GUIStyle _statusStripStyle;
        private GUIStyle _bakeHeaderStyle;
        private bool _stylesInitialized;

        private int _dropItemCount = 1;

        private enum WorldPlacementMode
        {
            Actor = 0,
            Interaction = 1,
        }

        private enum ActorPlacementSource
        {
            ActorDatabase = 0,
            DirectPrefab = 1,
            GroupPreset = 2,
        }

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

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/월드/맵/월드 배치 도구", priority = UPlaygroundMenuPriority.WorldMap)]
        public static void Open()
        {
            Open(WorldPlacementMode.Actor);
        }

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/월드/맵/NPC 배치 도구", priority = UPlaygroundMenuPriority.WorldMap + 1)]
        public static void OpenNpcPlacement()
        {
            var window = Open(WorldPlacementMode.Actor);
            window._actorSource = ActorPlacementSource.ActorDatabase;
            window._actorFilter = ActorType.NPC;
            window.RefreshActorDefinitions();
            window.SetPersistentStatus("NPC ActorDefinitionSO를 선택해 씬에 배치하세요.", MessageType.Info);
        }

        internal static void OpenActorPlacement()
        {
            Open(WorldPlacementMode.Actor);
        }

        internal static void OpenInteractionPlacement()
        {
            Open(WorldPlacementMode.Interaction);
        }

        private static GatheringPlacementEditorWindow Open(WorldPlacementMode mode)
        {
            var window = GetWindow<GatheringPlacementEditorWindow>();
            window.titleContent = new GUIContent("World Placement", EditorGUIUtility.IconContent("d_Prefab Icon").image);
            // Master-Detail(좌 리스트 / 우 상세) 레이아웃 최소 폭 확보.
            window.minSize = new Vector2(760f, 560f);
            window.SetMode(mode);
            window.Show();
            return window;
        }

        private void OnEnable()
        {
            _raycastMask = CreateDefaultRaycastMask();
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            LoadPrefs();
            TryAutoLoadActorDatabase();
            RefreshActorDefinitions();
            RefreshInteractableDatas();
            RefreshItemDatas();
            RefreshGroupPresets();
            RefreshRuleProfiles();
            RefreshBakedDataAssets();
            SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
        }

        private void OnDisable()
        {
            if (_brushStrokeScope != null)
                EndBrushStroke();

            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            SaveFoldoutPrefs();
        }

        public void CreateGUI()
        {
            UPlayGround.EditorTools.UPlaygroundEditorUX.BuildLegacyWindow(
                rootVisualElement, "월드 배치 도구",
                "액터·상호작용·아이템 배치 규칙과 씬 브러시 작업 상태를 한 화면에서 관리합니다.",
                "d_Prefab Icon", DrawLegacyGUI, "up-world-placement");
        }

        private void DrawLegacyGUI()
        {
            InitStyles();
            HandleWindowShortcuts();

            DrawTopToolbar();
            DrawStatusStrip();

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(LeftPanelWidth), GUILayout.ExpandHeight(true)))
                DrawLeftPanel();

            DrawVerticalSeparator();

            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
                DrawRightPanel();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>모드 상태 + 세션 카운트 + 주요 액션을 한 줄로 묶은 상단 툴바.</summary>
        private void DrawTopToolbar()
        {
            bool canPlace = CanPlace(out _);
            Color barColor = _placementMode
                ? canPlace ? new Color(0.22f, 0.48f, 0.28f) : new Color(0.52f, 0.32f, 0.14f)
                : new Color(0.22f, 0.22f, 0.22f);

            Rect barRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(30f));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(barRect, barColor);

            GUILayout.Space(10f);
            string modeText = _placementMode ? "배치 모드 ON" : "배치 모드 OFF";
            GUILayout.Label($"{modeText}  ·  세션 {_sessionPlacementCount}개", _statusTextStyle, GUILayout.Height(30f));

            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.VerticalScope(GUILayout.Height(30f)))
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.HorizontalScope())
                {
                    Color previousBg = GUI.backgroundColor;
                    using (new EditorGUI.DisabledScope(!HasSelectedPlacementData()))
                    {
                        GUI.backgroundColor = _placementMode
                            ? new Color(0.9f, 0.45f, 0.38f)
                            : new Color(0.5f, 0.85f, 0.55f);
                        if (GUILayout.Button(_placementMode ? "■ 배치 중지" : "▶ 배치 시작", GUILayout.Width(92f)))
                        {
                            _placementMode = !_placementMode;
                            SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
                            SceneView.RepaintAll();
                        }
                    }
                    GUI.backgroundColor = previousBg;

                    DrawRuleProfileToolbar();

                    if (GUILayout.Button(new GUIContent("규칙 저장", "현재 배치 규칙을 프로필 에셋으로 저장"), GUILayout.Width(66f)))
                        SaveCurrentSettingsAsProfile();

                    if (GUILayout.Button(new GUIContent("새로고침", "데이터 새로고침"), GUILayout.Width(62f)))
                    {
                        RefreshActorDefinitions();
                        RefreshInteractableDatas();
                        RefreshItemDatas();
                        RefreshGroupPresets();
                        RefreshRuleProfiles();
                        RefreshBakedDataAssets();
                    }

                    using (new EditorGUI.DisabledScope(GetSelectedPingObject() == null))
                    {
                        if (GUILayout.Button(new GUIContent("Ping", "선택 데이터 Ping"), GUILayout.Width(44f)))
                            EditorGUIUtility.PingObject(GetSelectedPingObject());
                    }
                }
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(6f);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>준비 상태 또는 임시 상태 메시지를 한 줄로 보여주는 얇은 스트립.</summary>
        private void DrawStatusStrip()
        {
            string message = ShouldShowTemporaryStatus() ? _statusMessage : BuildReadinessMessage();
            MessageType type = ShouldShowTemporaryStatus() ? _statusType : GetReadinessMessageType();

            Rect rect = EditorGUILayout.GetControlRect(false, 20f);
            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f));

            _statusStripStyle.normal.textColor = type switch
            {
                MessageType.Error => new Color(0.95f, 0.55f, 0.5f),
                MessageType.Warning => new Color(0.95f, 0.78f, 0.35f),
                _ => new Color(0.75f, 0.75f, 0.75f),
            };

            GUI.Label(new Rect(rect.x + 8f, rect.y, rect.width - 16f, rect.height), message, _statusStripStyle);
        }

        private static void DrawVerticalSeparator()
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.Width(1f), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));
        }

        /// <summary>좌측 패널: 모드 탭 + 검색 + 데이터 리스트. 무엇을 배치할지 고르는 영역.</summary>
        private void DrawLeftPanel()
        {
            DrawWorldPlacementModeTabs();
            EditorGUILayout.Space(2f);

            switch (_worldPlacementMode)
            {
                case WorldPlacementMode.Actor:
                    DrawActorListPanel();
                    break;
                case WorldPlacementMode.Interaction:
                    DrawInteractionListPanel();
                    break;
            }

            GUILayout.Label(
                "선택 시 배치 모드 자동 ON · Esc 종료 · Ctrl+F 검색 · 1~5 최근 사용",
                EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>우측 패널: 선택 상세(상단 고정) + 배치 옵션(스크롤) + Bake(하단 고정).</summary>
        private void DrawRightPanel()
        {
            DrawSelectionHeader();

            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll, GUILayout.ExpandHeight(true));
            if (_worldPlacementMode == WorldPlacementMode.Actor)
            {
                DrawActorCommonOptions();
                if (IsGroupPresetMode)
                    DrawGroupPresetSettings();
                else if (IsMonsterActorPrefab(GetActorPrefab()))
                    DrawMonsterGroupSection();
                DrawActorPlacementRules();
                DrawActorSourceSettings();
            }
            else
            {
                DrawTargetSection();
                DrawPlacementSettings();
            }

            DrawSceneInventorySection();
            EditorGUILayout.EndScrollView();

            DrawRuntimeDataActions();
        }

        private void DrawWorldPlacementModeTabs()
        {
            EditorGUILayout.Space(6f);
            EditorGUI.BeginChangeCheck();
            _worldPlacementMode = (WorldPlacementMode)GUILayout.Toolbar(
                (int)_worldPlacementMode,
                new[] { "Actor / Prefab", "Interaction / Item" });
            if (EditorGUI.EndChangeCheck())
            {
                _placementMode = false;
                SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
                SceneView.RepaintAll();
            }
        }

        /// <summary>우측 상단 고정: 지금 무엇이 배치될지를 항상 보여주는 선택 상세 패널.</summary>
        private void DrawSelectionHeader()
        {
            Rect rect = EditorGUILayout.BeginVertical();
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(0.14f, 0.19f, 0.28f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), new Color(0.23f, 0.45f, 0.71f));
            }

            GUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10f);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("선택 영역 — 배치될 대상", _selectionCaptionStyle);
                    GUILayout.Label(
                        HasSelectedPlacementData() ? GetSelectedPlacementTitle() : "선택된 항목이 없습니다",
                        _selectionTitleStyle);

                    string detail = BuildSelectionDetailText();
                    if (!string.IsNullOrEmpty(detail))
                        GUILayout.Label(detail, _selectionDetailStyle);
                }
                GUILayout.Space(10f);
            }
            GUILayout.Space(8f);
            EditorGUILayout.EndVertical();
        }

        private string BuildSelectionDetailText()
        {
            if (_worldPlacementMode == WorldPlacementMode.Actor)
            {
                if (_actorSource == ActorPlacementSource.ActorDatabase)
                {
                    if (_selectedActorDefinition == null)
                        return "좌측 목록에서 배치할 액터를 선택하세요.";

                    if (_selectedActorDefinition.prefab == null)
                        return $"Actor ID {_selectedActorDefinition.actorId}  |  ⚠ prefab이 비어 있어 배치할 수 없습니다";

                    string groupSuffix = ShouldParentToGroup() ? $"  |  그룹 {_targetGroup.name}" : "";
                    return $"Actor ID {_selectedActorDefinition.actorId}  |  Type {_selectedActorDefinition.actorType}  |  Prefab {_selectedActorDefinition.prefab.name}{groupSuffix}";
                }

                return _directActorPrefab != null
                    ? $"직접 프리팹  |  {_directActorPrefab.name}{(ShouldParentToGroup() ? $"  |  그룹 {_targetGroup.name}" : "")}"
                    : "아래 '액터 배치 소스 설정'에서 직접 프리팹을 연결하세요.";
            }

            if (_placementKind == PlacementKind.Gathering)
            {
                if (_selectedData == null)
                    return "좌측 목록에서 배치할 상호작용 데이터를 선택하세요.";

                return $"{_selectedData.interactionObjectType}  |  HP {_selectedData.hp}  |  생성: {(_prefab != null ? _prefab.name : "기본 GameObject")}";
            }

            if (_selectedItem == null)
                return "좌측 목록에서 배치할 아이템을 선택하세요.";

            return $"ID {_selectedItem.itemId}  |  {_selectedItem.itemType}  x{_dropItemCount}  |  생성: {(_dropItemPrefab != null ? _dropItemPrefab.name : "기본 GameObject")}";
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
                _addPlacementMetadata = EditorGUILayout.Toggle("Add Placement Metadata", _addPlacementMetadata);
                using (new EditorGUI.DisabledScope(!_addPlacementMetadata))
                    _placementBakeMode = (WorldPlacementMetadata.PlacementBakeMode)EditorGUILayout.EnumPopup("Bake Mode", _placementBakeMode);
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

            DrawBrushSettings();
        }

        private void PlaceCurrent()
        {
            if (!CanPlace(out string reason))
            {
                SetTemporaryStatus(reason, MessageType.Warning);
                return;
            }

            string undoLabel = _worldPlacementMode switch
            {
                WorldPlacementMode.Actor => "Place Actor",
                _ => "Place Interaction",
            };

            // 배치 1건은 생성/부모연결/컴포넌트 추가로 Undo 엔트리가 여러 개 쌓인다.
            // 사용자에게는 '배치 1회 = Ctrl+Z 1회'로 보여야 하므로 하나의 그룹으로 묶는다.
            using var undoScope = new PlacementUndoScope(undoLabel);
            if (PlaceCurrentInternal())
                undoScope.Complete();
        }

        /// <summary>실제 배치 수행. 성공 시 true. false를 반환하면 호출부에서 Undo 그룹을 롤백한다.</summary>
        private bool PlaceCurrentInternal()
        {
            Transform parent = ResolveParent();
            Quaternion rotation = BuildPlacementRotation();
            PlacementInstance placement = CreateInstance();
            GameObject instance = placement.Root;

            if (instance == null)
                return false;

            string undoName = _worldPlacementMode switch
            {
                WorldPlacementMode.Actor => "Actor Placement",
                _ => "Interaction Placement",
            };
            Undo.RegisterCreatedObjectUndo(instance, undoName);
            if (parent != null)
                Undo.SetTransformParent(instance.transform, parent, "World Placement Parent");

            // Parent가 위치/회전/스케일을 가진 경우를 대비해 Parent 연결 이후 월드 배치 좌표를 확정한다.
            instance.transform.SetPositionAndRotation(_previewPosition, rotation);

            if (_worldPlacementMode == WorldPlacementMode.Actor)
            {
                ApplyActorDefinitionIfNeeded(instance);
            }
            else
            {
                ApplyInteractableLayer(instance);
                StickInstanceToSurface(placement);
                SetupColliderIfNeeded(instance);
                ApplyPlacementData(instance);
                AddSceneEntityIdIfNeeded(instance);
            }

            AddPlacementMetadataIfNeeded(instance);

            if (_selectAfterPlace)
                Selection.activeGameObject = instance;

            _sessionPlacementCount++;
            if (_worldPlacementMode == WorldPlacementMode.Interaction && _placementKind == PlacementKind.Gathering)
                AddRecentData(_selectedData);
            SetTemporaryStatus($"배치 완료 (이번 세션 {_sessionPlacementCount}개)", MessageType.Info);
            EditorSceneManager.MarkSceneDirty(instance.scene);
            Repaint();
            return true;
        }

        private PlacementInstance CreateInstance()
        {
            if (_worldPlacementMode == WorldPlacementMode.Actor)
            {
                GameObject actorPrefab = GetActorPrefab();
                if (actorPrefab == null)
                    return new PlacementInstance(null, null, moveSurfaceTargetOnly: false);

                var actorInstance = InstantiatePrefab(actorPrefab);
                return new PlacementInstance(actorInstance, actorInstance, moveSurfaceTargetOnly: false);
            }

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

        private void ApplyPlacementData(GameObject instance)
        {
            if (_worldPlacementMode == WorldPlacementMode.Actor)
            {
                ApplyActorDefinitionIfNeeded(instance);
                return;
            }

            if (_placementKind == PlacementKind.Gathering)
            {
                ApplyGatheringData(instance);
                return;
            }

            ApplyDropItemData(instance);
        }

        private void ApplyActorDefinitionIfNeeded(GameObject instance)
        {
            if (_actorSource != ActorPlacementSource.ActorDatabase || _selectedActorDefinition == null)
                return;

            var actor = instance.GetComponent<GameActor>();
            if (actor == null)
            {
                Debug.LogWarning($"[WorldPlacement] '{instance.name}'에 GameActor 컴포넌트가 없어 actorId를 주입하지 못했습니다.", instance);
                return;
            }

            var serializedActor = new SerializedObject(actor);
            var actorIdProperty = serializedActor.FindProperty("_actorId");
            if (actorIdProperty == null)
            {
                Debug.LogWarning($"[WorldPlacement] '{instance.name}'에서 _actorId 프로퍼티를 찾지 못했습니다.", instance);
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
                    Debug.LogWarning($"[WorldPlacement] '{instance.name}'에서 NPC _data 프로퍼티를 찾지 못했습니다.", instance);
                }
            }

            EditorUtility.SetDirty(actor);
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

        private void AddPlacementMetadataIfNeeded(GameObject instance)
        {
            if (!_addPlacementMetadata)
                return;

            var metadata = instance.GetComponent<WorldPlacementMetadata>();
            if (metadata == null)
                metadata = Undo.AddComponent<WorldPlacementMetadata>(instance);
            else
                Undo.RecordObject(metadata, "Set World Placement Metadata");

            metadata.EditorSetPlacementInfo(
                GetPlacementSourceKind(),
                GetPlacementSourceId(),
                _placementBakeMode,
                cellId: "",
                randomSeed: UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                initiallyActive: instance.activeSelf);
            EditorUtility.SetDirty(metadata);
        }

        private WorldPlacementMetadata.PlacementSourceKind GetPlacementSourceKind()
        {
            if (_worldPlacementMode == WorldPlacementMode.Actor)
            {
                return _actorSource == ActorPlacementSource.ActorDatabase
                    ? WorldPlacementMetadata.PlacementSourceKind.ActorDefinition
                    : WorldPlacementMetadata.PlacementSourceKind.DirectPrefab;
            }

            return _placementKind == PlacementKind.Gathering
                ? WorldPlacementMetadata.PlacementSourceKind.GatheringData
                : WorldPlacementMetadata.PlacementSourceKind.DropItemData;
        }

        private string GetPlacementSourceId()
        {
            if (_worldPlacementMode == WorldPlacementMode.Actor)
            {
                if (_actorSource == ActorPlacementSource.ActorDatabase)
                    return _selectedActorDefinition != null ? _selectedActorDefinition.actorId : "";

                return GetAssetGuid(_directActorPrefab);
            }

            return _placementKind == PlacementKind.Gathering
                ? GetAssetGuid(_selectedData)
                : GetAssetGuid(_selectedItem);
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
            // 몬스터 그룹 지정은 Parent/루트 옵션보다 우선한다.
            if (ShouldParentToGroup())
                return _targetGroup.transform;

            if (_parent != null)
                return _parent;

            if (!_autoCreateRoot)
                return null;

            string rootName = _worldPlacementMode switch
            {
                WorldPlacementMode.Actor => ActorPlacementRootName,
                _ => _placementKind == PlacementKind.Gathering ? DefaultRootName : DropItemRootName,
            };
            return GetOrCreatePlacementRoot(rootName);
        }

        private static Transform GetOrCreatePlacementRoot(string rootName)
        {
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

        private void RefreshActorDefinitions()
        {
            _actorDefinitions.Clear();

            if (_actorDatabase == null)
                TryAutoLoadActorDatabase();

            if (_actorDatabase == null)
            {
                Repaint();
                return;
            }

            foreach (var definition in _actorDatabase.All)
            {
                if (definition != null)
                    _actorDefinitions.Add(definition);
            }

            _actorDefinitions.Sort((a, b) =>
            {
                string left = string.IsNullOrEmpty(a.actorId) ? a.name : a.actorId;
                string right = string.IsNullOrEmpty(b.actorId) ? b.name : b.actorId;
                return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
            });

            if (_selectedActorDefinition == null && _actorDefinitions.Count > 0)
                SelectActorDefinition(_actorDefinitions[0], armPlacement: false);

            if (_selectedActorDefinition != null && !_actorDefinitions.Contains(_selectedActorDefinition))
                SelectActorDefinition(_actorDefinitions.Count > 0 ? _actorDefinitions[0] : null, armPlacement: false);

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

        private void SelectActorDefinition(ActorDefinitionSO definition, bool armPlacement = true)
        {
            _selectedActorDefinition = definition;
            _actorSource = ActorPlacementSource.ActorDatabase;

            if (armPlacement && definition != null && !_placementMode)
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

            if (HasBlockingIssue())
            {
                reason = "배치 검증에 실패했습니다. 프리뷰의 경고 내용을 확인하세요.";
                return false;
            }

            reason = null;
            return true;
        }

        private string BuildReadinessMessage()
        {
            if (!HasSelectedPlacementData())
                return GetSelectionRequiredMessage();

            if (_worldPlacementMode == WorldPlacementMode.Actor)
            {
                if (GetActorPrefab() == null)
                    return "배치할 ActorDefinitionSO 또는 직접 프리팹을 선택하세요.";

                return "Actor/Prefab 배치 준비 완료 - 씬 뷰 클릭으로 배치하세요. Esc 종료, Ctrl/Cmd+Z 취소.";
            }

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

            if (_worldPlacementMode == WorldPlacementMode.Actor)
                return GetActorPrefab() == null ? MessageType.Warning : MessageType.None;

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
            InvalidatePlacementQueryCache();
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
            if (_worldPlacementMode != WorldPlacementMode.Interaction)
                return false;

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
            _actorSourceFoldout = EditorPrefs.GetBool(SourceFoldoutPrefsKey, false);
            _bakeFoldout = EditorPrefs.GetBool(BakeFoldoutPrefsKey, true);
        }

        private void SaveRecentDataGuids()
        {
            EditorPrefs.SetString(RecentPrefsKey, string.Join(PrefsSeparator.ToString(), _recentDataGuids));
        }

        private void SaveFoldoutPrefs()
        {
            EditorPrefs.SetBool(AttachFoldoutPrefsKey, _attachOptionsFoldout);
            EditorPrefs.SetBool(PlacementFoldoutPrefsKey, _placementRulesFoldout);
            EditorPrefs.SetBool(SourceFoldoutPrefsKey, _actorSourceFoldout);
            EditorPrefs.SetBool(BakeFoldoutPrefsKey, _bakeFoldout);
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

        private bool ShouldShowDefinition(ActorDefinitionSO definition)
        {
            if (definition == null)
                return false;

            if (_actorFilter != ActorType.None && (definition.actorType & _actorFilter) == 0)
                return false;

            if (string.IsNullOrEmpty(_actorSearchFilter))
                return true;

            return ContainsIgnoreCase(definition.actorId, _actorSearchFilter)
                || ContainsIgnoreCase(definition.displayName, _actorSearchFilter)
                || ContainsIgnoreCase(definition.name, _actorSearchFilter);
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

            if (_worldPlacementMode == WorldPlacementMode.Actor)
            {
                _directActorPrefab = selected;
                _actorSource = ActorPlacementSource.DirectPrefab;
            }
            else if (_placementKind == PlacementKind.Gathering)
            {
                _prefab = selected;
            }
            else
            {
                _dropItemPrefab = selected;
            }

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
            if (_worldPlacementMode == WorldPlacementMode.Actor)
            {
                if (_actorSource == ActorPlacementSource.ActorDatabase && _selectedActorDefinition != null)
                {
                    string actorName = string.IsNullOrEmpty(_selectedActorDefinition.displayName)
                        ? _selectedActorDefinition.actorId
                        : _selectedActorDefinition.displayName;
                    return actorName;
                }

                return _directActorPrefab != null ? _directActorPrefab.name : "PlacedActor";
            }

            if (_placementKind == PlacementKind.DropItem)
                return $"DropItem_{GetItemTitle(_selectedItem)}";

            string baseName = _selectedData == null
                ? "GatheringActor"
                : GetDataTitle(_selectedData);

            return $"Gathering_{baseName}";
        }



        private bool HasSelectedPlacementData()
        {
            if (_worldPlacementMode == WorldPlacementMode.Actor)
            {
                if (_actorSource == ActorPlacementSource.GroupPreset)
                    return _selectedGroupPreset != null && _selectedGroupPreset.TotalInstanceCount > 0;

                return GetActorPrefab() != null;
            }

            return _placementKind == PlacementKind.Gathering
                ? _selectedData != null
                : _selectedItem != null;
        }

        private string GetSelectionRequiredMessage()
        {
            if (_worldPlacementMode == WorldPlacementMode.Actor)
                return _actorSource == ActorPlacementSource.ActorDatabase
                    ? "배치할 ActorDefinitionSO를 먼저 선택하세요."
                    : "배치할 직접 프리팹을 먼저 선택하세요.";

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

        private string GetSelectedPlacementTitle()
        {
            if (_worldPlacementMode == WorldPlacementMode.Actor)
            {
                if (_actorSource == ActorPlacementSource.GroupPreset)
                    return _selectedGroupPreset != null ? _selectedGroupPreset.DisplayName : "그룹 프리셋 미선택";

                return BuildObjectName();
            }

            return _placementKind == PlacementKind.Gathering
                ? GetDataTitle(_selectedData)
                : GetItemTitle(_selectedItem);
        }

        private UnityEngine.Object GetSelectedPingObject()
        {
            if (_worldPlacementMode == WorldPlacementMode.Actor)
                return _actorSource == ActorPlacementSource.ActorDatabase
                    ? _selectedActorDefinition
                    : _directActorPrefab;

            return _placementKind == PlacementKind.Gathering
                ? _selectedData
                : _selectedItem;
        }

        private GameObject GetActorPrefab()
        {
            return _actorSource == ActorPlacementSource.ActorDatabase
                ? _selectedActorDefinition != null ? _selectedActorDefinition.prefab : null
                : _directActorPrefab;
        }

        private static bool IsMonsterActorPrefab(GameObject prefab)
        {
            return prefab != null && prefab.GetComponent<MonsterActor>() != null;
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

        private void SetMode(WorldPlacementMode mode)
        {
            _worldPlacementMode = mode;
            _placementMode = false;
            SetPersistentStatus(BuildReadinessMessage(), GetReadinessMessageType());
            Repaint();
            SceneView.RepaintAll();
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

        private static string GetAssetGuid(UnityEngine.Object asset)
        {
            if (asset == null)
                return "";

            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? "" : AssetDatabase.AssetPathToGUID(path);
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

            _selectionCaptionStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = new Color(0.5f, 0.69f, 0.94f) },
            };

            _selectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = Color.white },
            };

            _selectionDetailStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                normal = { textColor = new Color(0.85f, 0.89f, 0.96f) },
            };

            _statusStripStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
            };

            var bakeHeaderColor = new Color(0.88f, 0.72f, 0.29f);
            _bakeHeaderStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
            };
            _bakeHeaderStyle.normal.textColor = bakeHeaderColor;
            _bakeHeaderStyle.onNormal.textColor = bakeHeaderColor;
            _bakeHeaderStyle.hover.textColor = bakeHeaderColor;
            _bakeHeaderStyle.onHover.textColor = bakeHeaderColor;
            _bakeHeaderStyle.active.textColor = bakeHeaderColor;
            _bakeHeaderStyle.onActive.textColor = bakeHeaderColor;

            _stylesInitialized = true;
        }
    }
}
#endif
