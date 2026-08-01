using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using UPlayGround.Data.Event;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 프로젝트 타입에 의존하지 않는 MotionSet 편집 및 프리뷰 창.
    /// 프로젝트 연결은 카탈로그, 프리뷰 대상 바인더, 패널 확장으로만 받는다.
    /// </summary>
    public sealed partial class MotionSetEditorWindow :
        EditorWindow,
        IMotionEditorContext
    {
        private const string PreviewCatalogPrefs =
            "MotionSetEditor.PreviewCatalog";
        private const string PreviewSubjectPrefs =
            "MotionSetEditor.PreviewSubject";
        private const string PendingPreviewSubjectPrefs =
            "MotionSetEditor.PendingPreviewSubject";

        private MotionSetAsset _asset;
        private IMotionSetCatalog _catalog;
        private string _selectedSlotId;
        private MotionSetDrawer _drawer;
        private const double CatalogRefreshInterval = 0.5d;
        private double _lastCatalogRefreshTime;
        private bool _catalogDirty = true;
        private string[] _catalogLabels = Array.Empty<string>();

        private MotionPreviewCatalogSO _previewCatalog;
        private int _selectedSubjectIndex = -1;
        private GameObject _manualTarget;
        private GameObject _spawnedTarget;
        private IMotionPreviewSubject _subject;
        [SerializeField] private AnimationClip _idleClip;
        private sealed class SceneTargetVisibilityState
        {
            public bool ActiveSelf;
            public Renderer[] Renderers;
            public bool[] RendererStates;
        }

        private readonly Dictionary<GameObject, SceneTargetVisibilityState>
            _sceneTargetVisibilityStates = new();

        private bool _isPlaying;
        private bool _isPaused;
        private bool _hasWindowFocus;
        private bool _loop;
        private bool _rootMotionEnabled = true;
        private float _playbackTime;
        private float _playbackSpeed = 1f;
        private float _playbackStopTime = -1f;
        private double _lastUpdateTime;
        private int _currentMotionIndex = -1;
        private AnimancerState _previewState;
        private float _previousPlaybackTime = -0.001f;
        private readonly HashSet<MotionEventBase> _executedEvents = new();
        private readonly HashSet<MotionEventBase> _activeEvents = new();
        private bool _rootMotionActive;
        private Vector3 _rootMotionInitialPosition;
        private Quaternion _rootMotionInitialRotation;
        private bool _previousApplyRootMotion;

        public MotionSetAsset Asset => _asset;
        public MotionSet CurrentSet => _asset != null ? _asset.motionSet : null;
        public Motion CurrentMotion
        {
            get
            {
                MotionSet set = CurrentSet;
                int index = _drawer?.selectedMotionIndex ?? -1;
                return set?.motions != null &&
                       index >= 0 &&
                       index < set.motions.Count
                    ? set.motions[index]
                    : null;
            }
        }

        public MotionEventBase SelectedEvent =>
            _drawer?.GetSelectedEvent(CurrentSet);
        public IMotionPreviewSubject Subject => _subject;
        public IMotionSetCatalog Catalog => _catalog;
        public string SelectedSlotId => _selectedSlotId;
        public float PlaybackTime => _playbackTime;
        public bool IsPlaying => _isPlaying && !_isPaused;

        public static void Open(MotionSetAsset asset)
        {
            MotionSetEditorWindow window = ShowWindow();
            window.SetCatalog(null, null, asset);
        }

        public static void Open(
            IMotionSetCatalog catalog,
            string slotId = null,
            MotionSetAsset asset = null)
        {
            MotionSetEditorWindow window = ShowWindow();
            window.SetCatalog(catalog, slotId, asset);
        }

        private static MotionSetEditorWindow ShowWindow()
        {
            MotionSetEditorWindow window = GetWindow<MotionSetEditorWindow>();
            window.titleContent = new GUIContent("애니메이션 에디터");
            window.minSize = new Vector2(560f, 480f);
            window.Show();
            return window;
        }

        private void OnEnable()
        {
            _drawer = new MotionSetDrawer(
                () => _asset,
                Repaint,
                (_, _) => Repaint());
            _drawer.onDrawEventToolPanel = DrawEventExtensionInspector;
            _drawer.onSelectionChanged = QueueEditorViewRefresh;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            SceneView.duringSceneGui += OnSceneGUI;
            RestorePreviewCatalog();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            SceneView.duringSceneGui -= OnSceneGUI;
            foreach (IMotionEditorPanel panel in MotionEditorExtensionRegistry.Panels)
            {
                if (panel is IMotionEditorPanelLifecycle lifecycle)
                    lifecycle.OnEditorClosed(this);
            }
            StopPlayback();
            ReleaseSubject();
            DisposeUIToolkit();
        }

        private void OnFocus()
        {
            // OnFocus 시점에는 EditorWindow.focusedWindow가 아직 이전 창일 수 있어
            // 포커스 여부를 콜백에서 직접 기록한다.
            _hasWindowFocus = true;
            RefreshInputLock();
        }

        private void OnLostFocus()
        {
            _hasWindowFocus = false;
            if (!_isPlaying)
                ReleaseInputLock();
        }

        private void OnProjectChange()
        {
            if (_previewCatalog == null)
                RestorePreviewCatalog();
            _catalogDirty = true;
            Repaint();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            MotionSetAsset next = (MotionSetAsset)EditorGUILayout.ObjectField(
                _asset,
                typeof(MotionSetAsset),
                false,
                GUILayout.MinWidth(240f));
            if (next != _asset)
                SetAsset(next);
            if (GUILayout.Button("저장", EditorStyles.toolbarButton, GUILayout.Width(52f)))
            {
                if (_asset != null)
                {
                    EditorUtility.SetDirty(_asset);
                    AssetDatabase.SaveAssetIfDirty(_asset);
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 카탈로그 재수집은 리플렉션과 AbilitySet 전체 순회를 포함하므로 비싸다.
        /// 매 repaint가 아니라 Layout 이벤트에서, 변경 표시가 있거나 최소 간격이
        /// 지난 경우에만 수행한다. Layout으로 제한해야 Layout/Repaint 사이에
        /// 슬롯 수가 바뀌어 IMGUI 컨트롤 수가 어긋나는 것도 막을 수 있다.
        /// </summary>
        private void RefreshCatalogCache(bool force)
        {
            if (_catalog == null)
            {
                _catalogLabels = Array.Empty<string>();
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (!force &&
                !_catalogDirty &&
                now - _lastCatalogRefreshTime < CatalogRefreshInterval)
                return;

            _catalog.Refresh();
            _lastCatalogRefreshTime = now;
            _catalogDirty = false;
            _catalogLabels = _catalog.Slots
                .Select(slot => string.IsNullOrEmpty(slot.GroupLabel)
                    ? slot.DisplayName
                    : $"{slot.GroupLabel}/{slot.DisplayName}")
                .ToArray();
        }

        private void DrawCatalog()
        {
            if (_catalog == null)
                return;

            if (Event.current.type == EventType.Layout)
                RefreshCatalogCache(false);
            DrawCatalogVariants();
            IReadOnlyList<MotionSetSlot> slots = _catalog.Slots;
            string[] labels = _catalogLabels.Length == slots.Count
                ? _catalogLabels
                : Array.Empty<string>();
            int current = FindSlotIndex(slots, _selectedSlotId);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.ObjectField(
                "카탈로그",
                _catalog.SourceAsset,
                typeof(UnityEngine.Object),
                false);
            EditorGUILayout.BeginHorizontal();
            if (labels.Length > 0)
            {
                int next = EditorGUILayout.Popup(
                    Mathf.Max(0, current),
                    labels,
                    GUILayout.MinWidth(220f));
                if (next != current || current < 0)
                    SelectSlot(slots[next].SlotId);
            }

            if (GUILayout.Button("빈 슬롯 추가", GUILayout.Width(92f)))
                ShowAssignableSlotMenu();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawCatalogVariants()
        {
            if (_catalog is not IMotionSetCatalogVariants variants)
                return;

            HashSet<string> subjectAxisIds =
                _subject is IMotionPreviewVariants subjectVariants
                    ? new HashSet<string>(
                        subjectVariants.Axes
                            .Where(axis => axis != null)
                            .Select(axis => axis.Id),
                        StringComparer.Ordinal)
                    : null;
            foreach (MotionPreviewAxis axis in variants.Axes)
            {
                if (axis?.Options == null || axis.Options.Count == 0)
                    continue;
                if (subjectAxisIds?.Contains(axis.Id) == true)
                    continue;

                string selected = variants.GetSelected(axis.Id);
                int current = FindAxisOptionIndex(axis, selected);
                int next = EditorGUILayout.Popup(
                    axis.DisplayName,
                    current,
                    axis.Options.Select(option => option.DisplayName).ToArray());
                if (next < 0 || next == current ||
                    !variants.Select(axis.Id, axis.Options[next].Id))
                    continue;

                string previousSlot = _selectedSlotId;
                RefreshCatalogCache(true);
                IReadOnlyList<MotionSetSlot> slots = _catalog.Slots;
                int slotIndex = FindSlotIndex(slots, previousSlot);
                if (slots.Count > 0)
                    SelectSlot(slots[Mathf.Max(0, slotIndex)].SlotId);
                else
                    SetAsset(null);
            }
        }

        private void DrawPreviewSubject()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            MotionPreviewCatalogSO nextCatalog =
                (MotionPreviewCatalogSO)EditorGUILayout.ObjectField(
                    "프리뷰 카탈로그",
                    _previewCatalog,
                    typeof(MotionPreviewCatalogSO),
                    false);
            if (nextCatalog != _previewCatalog)
            {
                _previewCatalog = nextCatalog;
                _selectedSubjectIndex = -1;
                SavePreviewCatalog();
            }

            DrawPreviewSceneControls();

            if (_previewCatalog != null && _previewCatalog.subjects.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                string[] names = _previewCatalog.subjects
                    .Select(entry => string.IsNullOrWhiteSpace(entry.displayName)
                        ? entry.id
                        : entry.displayName)
                    .ToArray();
                int nextIndex = EditorGUILayout.Popup(
                    Mathf.Max(0, _selectedSubjectIndex),
                    names,
                    GUILayout.MinWidth(150f));
                if (nextIndex != _selectedSubjectIndex)
                {
                    _selectedSubjectIndex = nextIndex;
                    EditorPrefs.SetString(
                        PreviewSubjectPrefs,
                        _previewCatalog.subjects[nextIndex].id ?? string.Empty);
                    if (!Application.isPlaying)
                        PreviewSelectedSubjectData();
                }
                MotionPreviewCatalogSO.SubjectEntry selected =
                    GetSelectedSubjectEntry();
                string actionLabel = selected?.source ==
                                     MotionPreviewCatalogSO.SubjectSource.ScenePrefab
                    ? "스폰"
                    : "씬 대상 연결";
                using (new EditorGUI.DisabledScope(
                           selected == null ||
                           selected.source ==
                           MotionPreviewCatalogSO.SubjectSource.ScenePrefab &&
                           !Application.isPlaying))
                {
                    if (GUILayout.Button(actionLabel, GUILayout.Width(88f)))
                        LoadSelectedSubject();
                }
                if (_spawnedTarget != null &&
                    GUILayout.Button("제거", GUILayout.Width(44f)))
                {
                    ReleaseSubject();
                    _manualTarget = null;
                }
                EditorGUILayout.EndHorizontal();
            }

            GameObject nextTarget = (GameObject)EditorGUILayout.ObjectField(
                "수동 대상",
                _manualTarget,
                typeof(GameObject),
                true);
            if (nextTarget != _manualTarget)
            {
                ReleaseSubject();
                _manualTarget = nextTarget;
                BindSubject(_manualTarget);
            }

            DrawVariantAxes();
            EditorGUILayout.EndVertical();
        }

        private void DrawVariantAxes()
        {
            if (_subject is not IMotionPreviewVariants variants)
                return;

            foreach (MotionPreviewAxis axis in variants.Axes)
            {
                if (axis?.Options == null || axis.Options.Count == 0)
                    continue;

                string selected = variants.GetSelected(axis.Id);
                int current = FindAxisOptionIndex(axis, selected);
                int next = EditorGUILayout.Popup(
                    axis.DisplayName,
                    current,
                    axis.Options.Select(option => option.DisplayName).ToArray());
                if (next < 0 || next == current)
                    continue;

                // 모델/장비 교체는 Animancer와 ActorAnimator 인스턴스를 바꿀 수 있다.
                // 기존 그래프의 재생 상태와 루트 모션 소유권을 먼저 정리한다.
                StopPlayback();
                if (variants.Select(axis.Id, axis.Options[next].Id))
                {
                    _subject.Refresh();
                    if (axis.AffectsCatalog && _subject.Catalog != null)
                        SetCatalog(_subject.Catalog, _selectedSlotId, null);
                }
            }
        }

        private void DrawPlayback()
        {
            bool compact = position.width < 980f;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            _idleClip = (AnimationClip)EditorGUILayout.ObjectField(
                "Idle",
                _idleClip,
                typeof(AnimationClip),
                false,
                GUILayout.MinWidth(180f));
            if (!compact)
                DrawPlaybackButtons();
            EditorGUILayout.EndHorizontal();

            if (compact)
            {
                EditorGUILayout.BeginHorizontal();
                DrawPlaybackButtons();
                EditorGUILayout.EndHorizontal();
            }
            if (_subject != null && _subject.Animancer == null)
            {
                EditorGUILayout.HelpBox(
                    "선택 대상의 활성 모델에서 AnimancerComponent를 찾지 못해 재생할 수 없습니다.",
                    MessageType.Warning);
            }
            EditorGUILayout.EndVertical();

            if (CurrentSet != null)
            {
                float duration = Mathf.Max(0.01f, CurrentSet.TotalDuration);
                float next = EditorGUILayout.Slider(
                    "시간",
                    _playbackTime,
                    0f,
                    duration);
                if (!Mathf.Approximately(next, _playbackTime))
                    SetPlaybackTime(next);
            }
        }

        private void DrawPlaybackButtons()
        {
            bool canPlay = _subject?.Animancer != null && CurrentSet != null;
            using (new EditorGUI.DisabledScope(!canPlay))
            {
                string label = _isPlaying && !_isPaused
                    ? "Ⅱ 일시정지"
                    : "▶ 재생";
                if (GUILayout.Button(label, GUILayout.Width(88f), GUILayout.Height(22f)))
                {
                    if (_isPlaying)
                        TogglePause();
                    else
                        StartPlayback();
                }
                if (GUILayout.Button("■ 정지", GUILayout.Width(62f), GUILayout.Height(22f)))
                    StopPlayback();
            }
            _loop = GUILayout.Toggle(_loop, "반복", GUILayout.Width(48f));
            if (_subject is IMotionPreviewRootMotion)
                _rootMotionEnabled = GUILayout.Toggle(
                    _rootMotionEnabled,
                    "루트 모션",
                    GUILayout.Width(78f));
            _playbackSpeed = EditorGUILayout.Slider(
                _playbackSpeed,
                0.05f,
                3f,
                GUILayout.MinWidth(130f));
        }

        private void DrawPreviewSceneControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                SceneAsset scene = _previewCatalog != null
                    ? _previewCatalog.previewScene
                    : null;
                EditorGUILayout.ObjectField(
                    "프리뷰 씬",
                    scene,
                    typeof(SceneAsset),
                    false);
                using (new EditorGUI.DisabledScope(
                           scene == null || Application.isPlaying))
                {
                    if (GUILayout.Button("씬 열기", GUILayout.Width(64f)))
                        OpenPreviewScene();
                }
                string playLabel = Application.isPlaying
                    ? "Play 종료"
                    : "씬에서 Play";
                using (new EditorGUI.DisabledScope(
                           !Application.isPlaying && scene == null))
                {
                    if (GUILayout.Button(playLabel, GUILayout.Width(78f)))
                    {
                        if (Application.isPlaying)
                            EditorApplication.ExitPlaymode();
                        else
                            PlayPreviewScene();
                    }
                }
            }
        }

        private void DrawEventExtensionInspector(MotionEventBase motionEvent)
        {
            MotionEditorExtensionRegistry
                .FindSceneEditor(motionEvent)
                ?.OnInspectorGUI(motionEvent, this);
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_subject is IMotionPreviewStatusOverlay status &&
                _subject.Root != null)
            {
                Handles.Label(
                    _subject.Root.transform.position + Vector3.up * 2f,
                    status.GetSceneStatusText());
            }

            foreach (IMotionEditorPanel panel in MotionEditorExtensionRegistry.Panels)
            {
                if (panel.IsAvailable(this))
                    panel.OnSceneGUI(this);
            }

            IMotionEventSceneEditor editor =
                MotionEditorExtensionRegistry.FindSceneEditor(SelectedEvent);
            if (editor != null && editor.OnSceneGUI(SelectedEvent, this))
            {
                if (_asset != null)
                    EditorUtility.SetDirty(_asset);
                Repaint();
            }
        }

        private void SetCatalog(
            IMotionSetCatalog catalog,
            string slotId,
            MotionSetAsset asset)
        {
            _catalog = catalog;
            _catalogDirty = true;
            RefreshCatalogCache(true);
            _selectedSlotId = slotId;
            if (asset != null)
            {
                SetAsset(asset);
                return;
            }

            IReadOnlyList<MotionSetSlot> slots = _catalog?.Slots;
            if (slots == null || slots.Count == 0)
            {
                SetAsset(null);
                RefreshEditorViews();
                return;
            }

            int index = FindSlotIndex(slots, slotId);
            SelectSlot(slots[Mathf.Max(0, index)].SlotId);
        }

        private void SelectSlot(string slotId)
        {
            _selectedSlotId = slotId;
            SetAsset(_catalog?.Resolve(slotId));
        }

        private void SetAsset(MotionSetAsset asset)
        {
            if (_asset == asset)
            {
                RefreshEditorViews();
                return;
            }

            StopPlayback();
            _asset = asset;
            if (_drawer != null)
            {
                _drawer.selectedMotionIndex = -1;
                _drawer.selectedLayerIndex = -1;
                _drawer.selectedEventMotionIndex = -1;
                _drawer.selectedEventIndex = -1;
                _drawer.selectedEventIsSetEvent = false;
                _drawer.overlayTracks = null;
            }
            RefreshEditorViews();
            Repaint();
        }

        private void ShowAssignableSlotMenu()
        {
            if (_catalog == null)
                return;

            GenericMenu menu = new();
            foreach (MotionSetSlot slot in _catalog.AssignableSlots)
            {
                MotionSetSlot captured = slot;
                menu.AddItem(
                    new GUIContent($"{slot.GroupLabel}/{slot.DisplayName}"),
                    false,
                    () =>
                    {
                        string sourcePath =
                            AssetDatabase.GetAssetPath(_catalog.SourceAsset);
                        string directory = string.IsNullOrEmpty(sourcePath)
                            ? "Assets"
                            : System.IO.Path.GetDirectoryName(sourcePath)
                                ?.Replace('\\', '/');
                        MotionSetAsset created =
                            _catalog.CreateAndAssign(captured.SlotId, directory);
                        if (created != null)
                            SetCatalog(_catalog, captured.SlotId, created);
                    });
            }

            menu.ShowAsContext();
        }

        private void LoadSelectedSubject()
        {
            if (_previewCatalog == null ||
                _selectedSubjectIndex < 0 ||
                _selectedSubjectIndex >= _previewCatalog.subjects.Count)
                return;

            MotionPreviewCatalogSO.SubjectEntry entry =
                _previewCatalog.subjects[_selectedSubjectIndex];
            ReleaseSubject();
            GameObject target;
            if (entry.source == MotionPreviewCatalogSO.SubjectSource.ScenePresent)
            {
                target = GameObject.Find(entry.sceneObjectName);
            }
            else if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "[MotionSetEditor] 프리팹 프리뷰 대상은 Play Mode에서만 스폰할 수 있습니다.");
                return;
            }
            else if (entry.prefab != null)
            {
                ResolveSpawnPose(
                    entry,
                    out Vector3 spawnPosition,
                    out Quaternion spawnRotation);
                HideScenePresentTargets();
                target = Instantiate(entry.prefab);
                if (target != null)
                {
                    target.name = $"[MotionPreview] {entry.displayName}";
                    target.hideFlags |= HideFlags.DontSave;
                    target.transform.SetPositionAndRotation(
                        spawnPosition,
                        spawnRotation);
                    _spawnedTarget = target;
                }
                else
                {
                    RestoreScenePresentTargets();
                }
            }
            else
            {
                target = null;
            }

            if (entry.idleClip != null)
                _idleClip = entry.idleClip;
            _manualTarget = target;
            BindSubject(
                target,
                entry.source ==
                MotionPreviewCatalogSO.SubjectSource.ScenePrefab);
        }

        private void ResolveSpawnPose(
            MotionPreviewCatalogSO.SubjectEntry entry,
            out Vector3 position,
            out Quaternion rotation)
        {
            Transform anchor = FindScenePresentAnchor();
            if (anchor != null)
            {
                rotation = anchor.rotation;
                position = anchor.position +
                           rotation * entry.spawnOffset;
                return;
            }

            Camera camera = Camera.main;
            if (camera != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(
                    camera.transform.forward,
                    Vector3.up).normalized;
                if (forward.sqrMagnitude < 0.0001f)
                    forward = Vector3.forward;

                position = camera.transform.position + forward * 4f;
                if (Physics.Raycast(
                        position + Vector3.up * 10f,
                        Vector3.down,
                        out RaycastHit hit,
                        50f,
                        Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Ignore))
                {
                    position.y = hit.point.y;
                }
                position += Quaternion.LookRotation(forward, Vector3.up) *
                            entry.spawnOffset;
                rotation = Quaternion.LookRotation(-forward, Vector3.up);
                return;
            }

            position = entry.spawnOffset;
            rotation = Quaternion.identity;
        }

        private Transform FindScenePresentAnchor()
        {
            if (_previewCatalog?.subjects == null)
                return null;

            foreach (MotionPreviewCatalogSO.SubjectEntry candidate in
                     _previewCatalog.subjects)
            {
                if (candidate == null ||
                    candidate.source !=
                    MotionPreviewCatalogSO.SubjectSource.ScenePresent ||
                    string.IsNullOrWhiteSpace(candidate.sceneObjectName))
                {
                    continue;
                }

                GameObject sceneTarget =
                    GameObject.Find(candidate.sceneObjectName);
                if (sceneTarget != null)
                    return sceneTarget.transform;
            }
            return null;
        }

        private void PreviewSelectedSubjectData()
        {
            MotionPreviewCatalogSO.SubjectEntry entry =
                GetSelectedSubjectEntry();
            if (entry == null)
                return;

            if (entry.source ==
                MotionPreviewCatalogSO.SubjectSource.ScenePresent)
            {
                GameObject sceneTarget =
                    GameObject.Find(entry.sceneObjectName);
                if (sceneTarget != null)
                {
                    ReleaseSubject();
                    _manualTarget = sceneTarget;
                    BindSubject(sceneTarget);
                }
                return;
            }

            if (entry.prefab == null)
                return;

            ReleaseSubject();
            _manualTarget = null;
            IMotionPreviewSubject dataSubject =
                MotionPreviewSubjectBinderRegistry.Bind(entry.prefab);
            dataSubject?.Refresh();
            if (dataSubject?.Catalog != null)
                SetCatalog(dataSubject.Catalog, _selectedSlotId, null);
            if (entry.idleClip != null)
                _idleClip = entry.idleClip;
        }

        private MotionPreviewCatalogSO.SubjectEntry GetSelectedSubjectEntry()
        {
            return _previewCatalog != null &&
                   _selectedSubjectIndex >= 0 &&
                   _selectedSubjectIndex < _previewCatalog.subjects.Count
                ? _previewCatalog.subjects[_selectedSubjectIndex]
                : null;
        }

        private bool OpenPreviewScene()
        {
            if (_previewCatalog?.previewScene == null)
                return false;

            string scenePath =
                AssetDatabase.GetAssetPath(_previewCatalog.previewScene);
            if (string.IsNullOrEmpty(scenePath))
                return false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            return true;
        }

        private void PlayPreviewScene()
        {
            MotionPreviewCatalogSO.SubjectEntry entry = GetSelectedSubjectEntry();
            EditorPrefs.SetString(
                PendingPreviewSubjectPrefs,
                entry?.id ?? string.Empty);
            if (!OpenPreviewScene())
                return;
            EditorApplication.EnterPlaymode();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                StopPlayback();
                ReleaseSubject();
                _manualTarget = null;
                return;
            }

            if (state != PlayModeStateChange.EnteredPlayMode)
                return;

            string pendingId =
                EditorPrefs.GetString(PendingPreviewSubjectPrefs, string.Empty);
            EditorPrefs.DeleteKey(PendingPreviewSubjectPrefs);
            if (_previewCatalog == null)
                RestorePreviewCatalog();
            if (!string.IsNullOrEmpty(pendingId) && _previewCatalog != null)
            {
                int index = _previewCatalog.subjects.FindIndex(
                    candidate => candidate.id == pendingId);
                if (index >= 0)
                    _selectedSubjectIndex = index;
            }
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                    LoadSelectedSubject();
            };
        }

        private void BindSubject(GameObject target, bool spawned = false)
        {
            StopPlayback();
            ReleaseInputLock();
            if (_subject is IMotionPreviewSubjectSession previousSession)
                previousSession.OnPreviewReleased();
            _subject = MotionPreviewSubjectBinderRegistry.Bind(target);
            _subject?.Refresh();
            if (_subject is IMotionPreviewSubjectSession session)
                session.OnPreviewLoaded(spawned);
            if (_subject?.Catalog != null)
                SetCatalog(_subject.Catalog, _selectedSlotId, null);
            RefreshInputLock();
        }

        private void ReleaseSubject()
        {
            // 루트 모션 프리뷰는 Subject를 통해 KCC/Animator 상태를 복구한다.
            // Subject 참조를 끊기 전에 반드시 재생을 종료해야 씬 액터에
            // motor 비활성 또는 applyRootMotion 강제값이 남지 않는다.
            StopPlayback();
            ReleaseInputLock();
            if (_subject is IMotionPreviewSubjectSession session)
                session.OnPreviewReleased();
            _subject = null;
            ReleaseSpawnedTarget();
            RestoreScenePresentTargets();
        }

        private void ReleaseSpawnedTarget()
        {
            if (_spawnedTarget == null)
                return;

            if (Application.isPlaying)
                Destroy(_spawnedTarget);
            else
                DestroyImmediate(_spawnedTarget);
            _spawnedTarget = null;
        }

        private void HideScenePresentTargets()
        {
            RestoreScenePresentTargets();
            if (_previewCatalog?.subjects == null)
                return;

            Transform cameraAnchor = FindScenePresentAnchor();
            HashSet<string> sceneObjectNames = new(
                _previewCatalog.subjects
                    .Where(entry =>
                        entry != null &&
                        entry.source ==
                        MotionPreviewCatalogSO.SubjectSource.ScenePresent &&
                        !string.IsNullOrWhiteSpace(entry.sceneObjectName))
                    .Select(entry => entry.sceneObjectName),
                StringComparer.Ordinal);
            if (sceneObjectNames.Count == 0)
                return;

            foreach (GameObject sceneTarget in
                     FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (sceneTarget == null ||
                    !sceneTarget.scene.IsValid() ||
                    !sceneObjectNames.Contains(sceneTarget.name) ||
                    _sceneTargetVisibilityStates.ContainsKey(sceneTarget))
                {
                    continue;
                }

                SceneTargetVisibilityState state = new()
                {
                    ActiveSelf = sceneTarget.activeSelf,
                };
                _sceneTargetVisibilityStates.Add(sceneTarget, state);

                if (cameraAnchor != null &&
                    sceneTarget == cameraAnchor.gameObject)
                {
                    // 카메라 추적 대상인 씬 Player는 루트를 유지하고 외형만 숨긴다.
                    // 루트를 비활성화하면 CameraManager의 추적과 Look 입력도 함께
                    // 끊겨, 같은 위치에 생성한 프리뷰 액터가 화면에서 벗어난다.
                    state.Renderers =
                        sceneTarget.GetComponentsInChildren<Renderer>(true);
                    state.RendererStates =
                        new bool[state.Renderers.Length];
                    for (int i = 0; i < state.Renderers.Length; i++)
                    {
                        Renderer renderer = state.Renderers[i];
                        state.RendererStates[i] =
                            renderer != null && renderer.enabled;
                        if (renderer != null)
                            renderer.enabled = false;
                    }
                }
                else
                {
                    sceneTarget.SetActive(false);
                }
            }
        }

        private void RestoreScenePresentTargets()
        {
            foreach (KeyValuePair<GameObject, SceneTargetVisibilityState> pair in
                     _sceneTargetVisibilityStates)
            {
                GameObject target = pair.Key;
                SceneTargetVisibilityState state = pair.Value;
                if (target == null || state == null)
                    continue;

                target.SetActive(state.ActiveSelf);
                if (state.Renderers == null || state.RendererStates == null)
                    continue;

                int count = Mathf.Min(
                    state.Renderers.Length,
                    state.RendererStates.Length);
                for (int i = 0; i < count; i++)
                {
                    if (state.Renderers[i] != null)
                    {
                        state.Renderers[i].enabled =
                            state.RendererStates[i];
                    }
                }
            }
            _sceneTargetVisibilityStates.Clear();
        }

        private void AcquireInputLock()
        {
            if (Application.isPlaying &&
                _subject is IMotionPreviewInputLock inputLock)
            {
                inputLock.SetInputSuppressed(true, true);
                inputLock.ClearBufferedInput();
            }
        }

        private void RefreshInputLock()
        {
            // 에디터가 열려 있다는 이유만으로 게임플레이 입력을 계속 막지 않는다.
            // 프리뷰 재생 중이거나 이 창을 직접 조작할 때만 잠근다.
            if (Application.isPlaying &&
                (_isPlaying || _hasWindowFocus || focusedWindow == this))
            {
                AcquireInputLock();
            }
            else
            {
                ReleaseInputLock();
            }
        }

        private void ReleaseInputLock()
        {
            if (_subject is IMotionPreviewInputLock inputLock)
            {
                inputLock.SetInputSuppressed(false, true);
                inputLock.ClearBufferedInput();
            }
        }

        private void StartPlayback()
        {
            if (_subject?.Animancer == null || CurrentSet == null)
                return;

            if (_subject is IMotionPreviewPlaybackOwnership ownership)
                ownership.AcquirePreviewOwnership();

            float duration = CurrentSet.TotalDuration;
            if (duration > 0f &&
                _playbackTime >= duration - 0.0001f)
            {
                _playbackTime = 0f;
                _previousPlaybackTime = -0.001f;
                _drawer.cursorTime = 0f;
                _timelineView?.RefreshPlayback();
            }

            _isPlaying = true;
            _isPaused = false;
            RefreshInputLock();
            _playbackTime = Mathf.Clamp(
                _playbackTime,
                0f,
                duration);
            _lastUpdateTime = EditorApplication.timeSinceStartup;
            _currentMotionIndex = -1;
            ResetEventRuntime(true);
            _previousPlaybackTime = _playbackTime - 0.001f;
            BeginRootMotionPreview();
            UpdatePreviewPose();
            ExecuteActiveEvents();
            NotifyPlaybackState(MotionPreviewPlaybackState.Playing);
        }

        private void TogglePause()
        {
            _isPaused = !_isPaused;
            if (_previewState != null)
            {
                _previewState.Speed = _isPaused
                    ? 0f
                    : CurrentMotionPlaybackSpeed() * _playbackSpeed;
                SyncOverlayBaseSpeed(_previewState.Speed);
            }
            _lastUpdateTime = EditorApplication.timeSinceStartup;
            NotifyPlaybackState(
                _isPaused
                    ? MotionPreviewPlaybackState.Paused
                    : MotionPreviewPlaybackState.Playing);
        }

        private void StopPlayback()
        {
            bool wasActive = _isPlaying || _isPaused;
            _playbackStopTime = -1f;
            if (_previewState != null)
                _previewState.Speed = 0f;
            ResetEventRuntime(true);
            _isPlaying = false;
            _isPaused = false;
            _currentMotionIndex = -1;
            _previewState = null;
            ClearOverlayPreviewLayers();
            if (_subject?.Animancer != null && _idleClip != null)
            {
                AnimancerState idleState = _subject.Animancer.Play(_idleClip);
                if (idleState != null)
                    idleState.Speed = 1f;
            }
            EndRootMotionPreview();
            if (_subject is IMotionPreviewPlaybackOwnership ownership)
                ownership.ReleasePreviewOwnership();
            RefreshInputLock();
            if (wasActive)
                NotifyPlaybackState(MotionPreviewPlaybackState.Stopped);
        }

        private void OnEditorUpdate()
        {
            if (!_isPlaying || _isPaused || CurrentSet == null)
                return;

            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - _lastUpdateTime);
            _lastUpdateTime = now;
            _previousPlaybackTime = _playbackTime;
            _playbackTime += delta * _playbackSpeed;
            float duration = CurrentSet.TotalDuration;
            float stopTime = _playbackStopTime >= 0f
                ? Mathf.Min(_playbackStopTime, duration)
                : duration;
            if (_playbackTime >= stopTime)
            {
                if (_loop && _playbackStopTime < 0f && duration > 0f)
                {
                    float wrappedTime = _playbackTime % duration;

                    // 래핑 전에 마지막 프레임부터 끝까지의 이벤트를 처리한다.
                    // 곧바로 시간을 0 근처로 옮기면 이 구간의 이벤트가 누락된다.
                    _playbackTime = duration;
                    UpdatePreviewPose();
                    ExecuteActiveEvents();

                    ResetEventRuntime(true);
                    ResetRootMotionPreview();
                    _playbackTime = wrappedTime;
                    _previousPlaybackTime = -0.001f;
                }
                else
                {
                    _playbackTime = stopTime;
                    UpdatePreviewPose();
                    ExecuteActiveEvents();
                    TickRootMotionPreview();
                    StopPlayback();
                    _timelineView?.RefreshPlayback();
                    Repaint();
                    return;
                }
            }

            UpdatePreviewPose();
            ExecuteActiveEvents();
            TickRootMotionPreview();
            _timelineView?.RefreshPlayback();
            Repaint();
        }

        private void UpdatePreviewPose()
        {
            MotionSet set = CurrentSet;
            AnimancerComponent animancer = _subject?.Animancer;
            if (set == null || animancer == null ||
                !set.GetMotionAtTime(_playbackTime, out int index, out float localTime) ||
                set.motions == null ||
                index < 0 ||
                index >= set.motions.Count)
                return;

            Motion motion = set.motions[index];
            if (motion?.motionClip == null)
                return;

            if (_currentMotionIndex != index || _previewState == null)
            {
                bool blendFromPrevious =
                    _isPlaying &&
                    !_isPaused &&
                    _currentMotionIndex >= 0 &&
                    _currentMotionIndex != index;
                _currentMotionIndex = index;
                int layerIndex = Mathf.Max(0, set.baseLayerIndex);
                AnimancerLayer layer = animancer.Layers[layerIndex];
                if (layerIndex > 0 && _idleClip != null)
                {
                    AnimancerLayer baseLayer = animancer.Layers[0];
                    baseLayer.Weight = 1f;
                    if (baseLayer.CurrentState == null ||
                        baseLayer.CurrentState.Clip != _idleClip)
                    {
                        baseLayer.Play(_idleClip, 0f);
                    }
                }
                AvatarMask mask = _subject.GetLayerMask(layerIndex);
                if (mask != null)
                    layer.Mask = mask;
                layer.Weight = 1f;
                _previewState = layer.Play(
                    motion.motionClip,
                    blendFromPrevious ? set.InternalBlendDuration : 0f);
            }

            float motionSpeed = motion.playbackSpeed > 0f
                ? motion.playbackSpeed
                : 1f;
            _previewState.Time =
                motion.ClipStartTime + localTime * motionSpeed;
            _previewState.Speed = _isPlaying && !_isPaused
                ? motionSpeed * _playbackSpeed
                : 0f;
            if (!_isPlaying || _isPaused)
                _previewState.Weight = 1f;
            SyncOverlayBaseSpeed(_previewState.Speed);
            _drawer.cursorTime = _playbackTime;
        }

        private float CurrentMotionPlaybackSpeed()
        {
            MotionSet set = CurrentSet;
            Motion motion = set?.motions != null &&
                            _currentMotionIndex >= 0 &&
                            _currentMotionIndex < set.motions.Count
                ? set.motions[_currentMotionIndex]
                : null;
            return motion != null && motion.playbackSpeed > 0f
                ? motion.playbackSpeed
                : 1f;
        }

        private void ClearOverlayPreviewLayers()
        {
            AnimancerComponent animancer = _subject?.Animancer;
            if (animancer == null)
                return;

            int previewLayer = Mathf.Max(0, CurrentSet?.baseLayerIndex ?? 0);
            if (previewLayer <= 0 || previewLayer >= animancer.Layers.Count)
                return;

            AnimancerLayer layer = animancer.Layers[previewLayer];
            layer?.Stop();
        }

        private void SyncOverlayBaseSpeed(float speed)
        {
            AnimancerComponent animancer = _subject?.Animancer;
            int previewLayer = Mathf.Max(0, CurrentSet?.baseLayerIndex ?? 0);
            if (animancer == null || previewLayer <= 0)
                return;

            AnimancerState baseState = animancer.Layers[0].CurrentState;
            if (baseState != null)
                baseState.Speed = speed;
        }

        private void ExecuteActiveEvents()
        {
            MotionSet set = CurrentSet;
            GameObject target = _subject?.Root;
            if (!Application.isPlaying || set == null || target == null)
                return;

            foreach ((MotionEventBase motionEvent, float offset) in EnumerateEvents(set))
            {
                if (motionEvent == null)
                    continue;

                float start = motionEvent.startTime + offset;
                bool crossedStart =
                    start > _previousPlaybackTime && start <= _playbackTime;
                if (crossedStart && _executedEvents.Add(motionEvent))
                {
                    try
                    {
                        float span = Mathf.Max(0.0001f, _playbackTime - _previousPlaybackTime);
                        float fraction = Mathf.Clamp01(
                            (start - _previousPlaybackTime) / span);
                        motionEvent.Execute(target, fraction);
                        _activeEvents.Add(motionEvent);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(
                            $"[MotionEditor] 이벤트 실행 실패: " +
                            $"{motionEvent.GetType().Name}\n{exception}");
                    }
                }
            }

            if (_activeEvents.Count == 0)
                return;

            HashSet<MotionEventBase> currentlyActive =
                new(set.GetActiveEventsAt(_playbackTime));
            foreach (MotionEventBase motionEvent in _activeEvents.ToArray())
            {
                if (currentlyActive.Contains(motionEvent))
                    continue;

                try
                {
                    motionEvent.OnCompleteEvent(target);
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"[MotionEditor] 이벤트 종료 실패: " +
                        $"{motionEvent.GetType().Name}\n{exception}");
                }
                _activeEvents.Remove(motionEvent);
            }
        }

        /// <summary>
        /// 이벤트와 소속 Motion의 누적 시작 오프셋을 함께 열거한다.
        /// 오프셋을 <see cref="MotionEventBase.globalStartTimeOffset"/>에 기록하면
        /// 직렬화 필드가 프리뷰만으로 변경되어 불필요한 에셋 diff가 생기므로
        /// 값으로 전달한다.
        /// </summary>
        private static IEnumerable<(MotionEventBase Event, float Offset)>
            EnumerateEvents(MotionSet set)
        {
            if (set.globalEvents != null)
            {
                foreach (MotionEventBase motionEvent in set.globalEvents)
                    yield return (motionEvent, 0f);
            }

            float offset = 0f;
            if (set.motions == null)
                yield break;

            foreach (Motion motion in set.motions)
            {
                if (motion == null)
                    continue;

                if (motion.events != null)
                {
                    foreach (MotionEventBase motionEvent in motion.events)
                        yield return (motionEvent, offset);
                }
                offset += motion.Duration;
            }
        }

        private void ResetEventRuntime(bool completeActive)
        {
            GameObject target = _subject?.Root;
            if (completeActive && target != null)
            {
                foreach (MotionEventBase motionEvent in _activeEvents)
                {
                    try
                    {
                        motionEvent?.OnCompleteEvent(target);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(
                            $"[MotionEditor] 이벤트 정리 실패: " +
                            $"{motionEvent?.GetType().Name}\n{exception}");
                    }
                }
            }

            _activeEvents.Clear();
            _executedEvents.Clear();
        }

        private void BeginRootMotionPreview()
        {
            if (!_rootMotionEnabled ||
                _subject is not IMotionPreviewRootMotion rootMotion ||
                _subject.Root == null)
                return;

            Transform target = _subject.Root.transform;
            _rootMotionInitialPosition = target.position;
            _rootMotionInitialRotation = target.rotation;
            Animator animator = _subject.Animancer?.Animator;
            if (animator != null)
            {
                _previousApplyRootMotion = animator.applyRootMotion;
                animator.applyRootMotion = true;
            }
            rootMotion.SetSimulationSuspended(true);
            _rootMotionActive = true;
        }

        private void TickRootMotionPreview()
        {
            if (!_rootMotionActive ||
                !_rootMotionEnabled ||
                _subject is not IMotionPreviewRootMotion rootMotion ||
                _subject.Root == null)
                return;

            Vector3 deltaPosition = rootMotion.DeltaPosition;
            Quaternion deltaRotation = rootMotion.DeltaRotation;
            Transform target = _subject.Root.transform;
            rootMotion.Teleport(
                target.position + deltaPosition,
                target.rotation * deltaRotation);
        }

        private void ResetRootMotionPreview()
        {
            if (!_rootMotionActive ||
                _subject is not IMotionPreviewRootMotion rootMotion)
                return;
            rootMotion.Teleport(
                _rootMotionInitialPosition,
                _rootMotionInitialRotation);
        }

        private void EndRootMotionPreview()
        {
            // 프리뷰가 서스펜드한 경우에만 복구한다.
            // 활성화된 적이 없는데 해제하면 다른 주체가 잡아 둔 상태를 덮어쓴다.
            if (_rootMotionActive &&
                _subject is IMotionPreviewRootMotion rootMotion)
            {
                rootMotion.Teleport(
                    _rootMotionInitialPosition,
                    _rootMotionInitialRotation);
                rootMotion.SetSimulationSuspended(false);
            }

            Animator animator = _subject?.Animancer?.Animator;
            if (_rootMotionActive && animator != null)
                animator.applyRootMotion = _previousApplyRootMotion;
            _rootMotionActive = false;
        }

        private void NotifyPlaybackState(MotionPreviewPlaybackState state)
        {
            foreach (IMotionEditorPanel panel in MotionEditorExtensionRegistry.Panels)
            {
                if (panel.IsAvailable(this))
                    panel.OnPlaybackStateChanged(this, state);
            }
        }

        // IMotionEditorContext.Repaint()는 EditorWindow.Repaint()가 그대로 구현한다.

        public void RecordUndo(string label)
        {
            if (_asset != null)
                Undo.RecordObject(_asset, label);
        }

        public void SetPlaybackTime(float time)
        {
            ResetEventRuntime(true);
            _playbackTime = Mathf.Clamp(
                time,
                0f,
                CurrentSet?.TotalDuration ?? 0f);
            _previousPlaybackTime = _playbackTime - 0.001f;
            UpdatePreviewPose();
            _subject?.Animancer?.Evaluate();
            _timelineView?.RefreshPlayback();
            Repaint();
        }

        public void Play()
        {
            if (_isPlaying)
            {
                if (_isPaused)
                    TogglePause();
                return;
            }

            StartPlayback();
        }

        public void Stop()
        {
            StopPlayback();
        }

        public void SetOverlayTracks(
            string groupTitle,
            List<MotionSetDrawer.OverlayTrack> tracks)
        {
            if (_drawer == null)
                return;
            _drawer.overlayGroupTitle = string.IsNullOrWhiteSpace(groupTitle)
                ? "확장 데이터"
                : groupTitle;
            _drawer.overlayTracks = tracks;
            _timelineView?.RefreshData(false);
        }

        /// <summary>
        /// 현재 선택된 옵션의 인덱스. 목록에 없으면 -1을 반환해
        /// 팝업이 "미선택"으로 표시되고 0번 항목도 명시적으로 고를 수 있게 한다.
        /// </summary>
        private static int FindAxisOptionIndex(
            MotionPreviewAxis axis,
            string selected)
        {
            for (int i = 0; i < axis.Options.Count; i++)
            {
                if (string.Equals(
                        axis.Options[i].Id,
                        selected,
                        StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        private void RestorePreviewCatalog()
        {
            string path = EditorPrefs.GetString(PreviewCatalogPrefs, string.Empty);
            _previewCatalog =
                AssetDatabase.LoadAssetAtPath<MotionPreviewCatalogSO>(path);
            if (_previewCatalog == null)
            {
                string fallbackPath = AssetDatabase.FindAssets(
                        "t:MotionPreviewCatalogSO")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(candidate => !string.IsNullOrEmpty(candidate));
                _previewCatalog =
                    AssetDatabase.LoadAssetAtPath<MotionPreviewCatalogSO>(
                        fallbackPath);
                if (_previewCatalog == null)
                    return;
                SavePreviewCatalog();
            }

            string selectedId =
                EditorPrefs.GetString(PreviewSubjectPrefs, string.Empty);
            _selectedSubjectIndex = _previewCatalog.subjects.FindIndex(
                entry => entry.id == selectedId);
            if (_selectedSubjectIndex < 0 && _previewCatalog.subjects.Count > 0)
                _selectedSubjectIndex = 0;
        }

        private void SavePreviewCatalog()
        {
            EditorPrefs.SetString(
                PreviewCatalogPrefs,
                _previewCatalog != null
                    ? AssetDatabase.GetAssetPath(_previewCatalog)
                    : string.Empty);
        }

        private static int FindSlotIndex(
            IReadOnlyList<MotionSetSlot> slots,
            string slotId)
        {
            if (slots == null || string.IsNullOrEmpty(slotId))
                return -1;
            for (int i = 0; i < slots.Count; i++)
            {
                if (string.Equals(
                        slots[i].SlotId,
                        slotId,
                        StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }
    }
}
