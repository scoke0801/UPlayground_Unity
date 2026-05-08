using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.P09Builder
{
    /// <summary>
    /// P09 Character Prefab Builder의 메인 EditorWindow.
    /// 탭 5개 (기본정보 / 외형 / 무기 / 스탯 / 미리보기)를 호스팅하고
    /// PrefabBuildPipeline을 호출해 프리팹을 빌드한다.
    /// 빌드 프리셋(BuildPreset) Save/Load 지원.
    /// </summary>
    public sealed class P09CharacterPrefabBuilderWindow : EditorWindow
    {
        private const string MENU_PATH = "Tools/P09 Builder/Character Prefab Builder";
        private const float MIN_WIDTH = 700f;
        private const float MIN_HEIGHT = 550f;
        private const float PreviewPanelWidth = 320f;
        private const double PreviewDebounceSeconds = 0.2d;

        private const string PresetFolder = "Assets/10.Datas/Generated/BuildPresets";

        [SerializeField] private CharacterBuildConfig _config = new CharacterBuildConfig();
        [SerializeField] private int _activeTabIndex = 0;
        [SerializeField] private bool _showPreview = true;

        private readonly List<IBuilderTab> _tabs = new();
        private readonly List<string> _buildLogs = new();
        private P09AssetCatalog _catalog;
        private IconResolver _iconResolver;
        private PreviewSceneController _preview;
        private PrefabBuildPipeline _pipeline;
        private NameSequenceRegistry _registry;

        private string _previewName = "";
        private string _statusMessage = "";
        private bool _isBuilding = false;
        private bool _previewDirty = true;
        private double _nextPreviewRebuildTime;
        private Vector2 _scrollPos;
        private Vector2 _logScrollPos;

        public string PreviewName => _previewName;

        [MenuItem(MENU_PATH, priority = 1100)]
        public static void Open()
        {
            var w = GetWindow<P09CharacterPrefabBuilderWindow>("P09 Builder");
            w.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
            w.Show();
        }

        private void OnEnable()
        {
            _catalog = new P09AssetCatalog();
            _catalog.Refresh();

            _iconResolver = new IconResolver();
            _iconResolver.Warmup();
            _preview = new PreviewSceneController();

            _registry = new NameSequenceRegistry();
            _registry.Load();

            _pipeline = new PrefabBuildPipeline();

            _tabs.Clear();
            _tabs.Add(new BasicInfoTab());
            _tabs.Add(new AppearanceTab());
            _tabs.Add(new WeaponTab());
            _tabs.Add(new StatsTab());
            _tabs.Add(new PreviewTab());

            foreach (var tab in _tabs)
                tab.Initialize(this, _catalog);

            P09AssetCatalogPostprocessor.CatalogRootChanged += OnCatalogRootChanged;

            if (_config == null) _config = new CharacterBuildConfig();
            if (_activeTabIndex < 0 || _activeTabIndex >= _tabs.Count)
                _activeTabIndex = 0;

            RegeneratePreviewName();
            MarkPreviewDirty();
        }

        private void OnDisable()
        {
            P09AssetCatalogPostprocessor.CatalogRootChanged -= OnCatalogRootChanged;
            _preview?.Dispose();
            _preview = null;
        }

        private void OnGUI()
        {
            HandlePreviewDebounce();

            DrawToolbar();
            DrawTabHeaders();

            EditorGUILayout.Space(2);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        if (_activeTabIndex >= 0 && _activeTabIndex < _tabs.Count)
                        {
                            _tabs[_activeTabIndex].OnGUI(_config, _catalog, _iconResolver);
                        }
                        if (check.changed)
                        {
                            RegeneratePreviewName();
                            MarkPreviewDirty();
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }

                if (_showPreview && !IsPreviewTabActive())
                {
                    DrawPreviewPanel(PreviewPanelWidth);
                }
            }

            DrawStatusBar();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("P09 Character Prefab Builder", EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("프리셋 저장", EditorStyles.toolbarButton))
                    SavePreset();

                if (GUILayout.Button("프리셋 불러오기", EditorStyles.toolbarButton))
                    LoadPreset();

                if (GUILayout.Button("카탈로그 새로고침", EditorStyles.toolbarButton))
                {
                    _catalog.Refresh();
                    _iconResolver?.ClearCache();
                    _iconResolver?.Warmup();
                    MarkPreviewDirty();
                    Repaint();
                }

                _showPreview = GUILayout.Toggle(_showPreview, "미리보기", EditorStyles.toolbarButton);

                using (new EditorGUI.DisabledScope(_isBuilding))
                {
                    var prevColor = GUI.color;
                    GUI.color = Color.green;
                    if (GUILayout.Button("  ▶  빌 드  ", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                        OnBuildClicked();
                    GUI.color = prevColor;
                }
            }
        }

        private void DrawTabHeaders()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < _tabs.Count; i++)
                {
                    bool active = (i == _activeTabIndex);
                    bool toggled = GUILayout.Toggle(active, _tabs[i].Title, EditorStyles.toolbarButton);
                    if (toggled && !active)
                    {
                        _activeTabIndex = i;
                        GUI.FocusControl(null);
                    }
                }
            }
        }

        private void DrawPreviewPanel(float width)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width)))
            {
                DrawPreviewControls();
            }
        }

        private bool IsPreviewTabActive()
        {
            return _activeTabIndex >= 0 &&
                   _activeTabIndex < _tabs.Count &&
                   _tabs[_activeTabIndex] is PreviewTab;
        }

        public void DrawPreviewControls()
        {
            EditorGUILayout.LabelField("Live Preview", EditorStyles.boldLabel);

            var rect = GUILayoutUtility.GetRect(300f, 420f, GUILayout.ExpandWidth(true));
            _preview?.Draw(rect);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("재빌드"))
                    ForceRebuildPreviewNow();
                if (GUILayout.Button("리셋"))
                    _preview?.ResetView();
                if (GUILayout.Button("SceneView"))
                    _preview?.OpenInSceneView(_config, _catalog);
            }

            if (_preview != null)
            {
                _preview.CameraFov = EditorGUILayout.Slider("FOV", _preview.CameraFov, 20f, 60f);
                _preview.BackgroundColor = EditorGUILayout.ColorField("배경", _preview.BackgroundColor);
            }

            DrawValidationSummary();
            DrawBuildLogConsole();
        }

        private void DrawStatusBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label($"생성 이름: {_previewName}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                var validationErrors = CollectValidationErrors();
                var message = validationErrors.Count > 0
                    ? $"검증 오류 {validationErrors.Count}개"
                    : _statusMessage;
                if (!string.IsNullOrEmpty(message))
                    GUILayout.Label(message, EditorStyles.miniLabel);
            }
        }

        private void OnBuildClicked()
        {
            if (_config == null)
            {
                EditorUtility.DisplayDialog("빌드 실패", "Config가 null 입니다.", "확인");
                return;
            }

            // 모든 탭 + Config 자체의 유효성 검증을 합산
            var errors = new List<string>(_config.Validate());
            foreach (var tab in _tabs)
            {
                var tabErrors = tab.Validate(_config);
                if (tabErrors != null) errors.AddRange(tabErrors);
            }

            if (errors.Count > 0)
            {
                EditorUtility.DisplayDialog("유효성 오류", string.Join("\n", errors), "확인");
                return;
            }

            var kindFolder = CharacterNameGenerator.GetKindFolderName(_config.ActorKind);
            var folder = PathConfig.GetPrefabFolder(kindFolder, _previewName);

            bool confirm = EditorUtility.DisplayDialog(
                "프리팹 빌드",
                $"'{_previewName}' 캐릭터 프리팹을 생성합니다.\n경로: {folder}\n\n계속하시겠습니까?",
                "빌드", "취소");
            if (!confirm) return;

            _isBuilding = true;
            _statusMessage = "빌드 중...";
            Repaint();

            try
            {
                var result = _pipeline.Build(_config);
                _buildLogs.Clear();
                _buildLogs.AddRange(result.Logs);
                if (result.Success)
                {
                    _statusMessage = $"완료: {result.PrefabPath}";
                    if (!string.IsNullOrEmpty(result.PrefabPath))
                        EditorUtility.RevealInFinder(result.PrefabPath);

                    // 시퀀스가 증가했으니 미리보기 이름 갱신
                    if (_registry != null)
                    {
                        _registry.Load();
                    }
                    RegeneratePreviewName();
                }
                else
                {
                    _statusMessage = $"실패: {result.ErrorMessage}";
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                        _buildLogs.Add(result.ErrorMessage);
                    EditorUtility.DisplayDialog("빌드 실패", result.ErrorMessage ?? "알 수 없는 오류", "확인");
                }
            }
            finally
            {
                _isBuilding = false;
                Repaint();
            }
        }

        /// <summary>
        /// 미리보기 이름을 재계산. 카운터를 증가시키지 않는다 (Preview = Peek 사용).
        /// </summary>
        public void RegeneratePreviewName()
        {
            if (_config == null) return;
            _previewName = CharacterNameGenerator.Preview(_config, _registry);
            Repaint();
        }

        public void MarkPreviewDirty()
        {
            _previewDirty = true;
            _nextPreviewRebuildTime = EditorApplication.timeSinceStartup + PreviewDebounceSeconds;
        }

        private void HandlePreviewDebounce()
        {
            if (!_showPreview || !_previewDirty)
                return;
            if (EditorApplication.timeSinceStartup < _nextPreviewRebuildTime)
            {
                Repaint();
                return;
            }

            RebuildPreviewNow();
        }

        private void RebuildPreviewNow()
        {
            _previewDirty = false;
            _preview?.Rebuild(_config, _catalog);
            Repaint();
        }

        private void ForceRebuildPreviewNow()
        {
            _previewDirty = false;
            _preview?.Rebuild(_config, _catalog, force: true);
            Repaint();
        }

        private void OnCatalogRootChanged()
        {
            _catalog?.Refresh();
            _iconResolver?.ClearCache();
            _iconResolver?.Warmup();
            MarkPreviewDirty();
            _statusMessage = "P09 카탈로그 변경 감지";
            Repaint();
        }

        private void DrawValidationSummary()
        {
            var errors = CollectValidationErrors();
            if (errors.Count == 0)
            {
                EditorGUILayout.HelpBox("검증 통과", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(string.Join("\n", errors), MessageType.Warning);
        }

        private void DrawBuildLogConsole()
        {
            EditorGUILayout.LabelField("Build Log", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinHeight(76f)))
            {
                _logScrollPos = EditorGUILayout.BeginScrollView(_logScrollPos, GUILayout.Height(70f));
                if (_buildLogs.Count == 0)
                {
                    EditorGUILayout.LabelField("로그 없음", EditorStyles.miniLabel);
                }
                else
                {
                    foreach (var log in _buildLogs)
                        EditorGUILayout.LabelField(log, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private List<string> CollectValidationErrors()
        {
            var errors = _config != null
                ? new List<string>(_config.Validate())
                : new List<string> { "Config가 null입니다." };
            foreach (var tab in _tabs)
            {
                var tabErrors = tab.Validate(_config);
                if (tabErrors != null)
                    errors.AddRange(tabErrors);
            }
            return errors;
        }

        // ---------- Preset Save/Load ----------

        private void SavePreset()
        {
            if (_config == null)
            {
                EditorUtility.DisplayDialog("저장 실패", "Config가 null 입니다.", "확인");
                return;
            }

            // 기본 폴더 생성 보장
            PathConfig.EnsureFolderExists(PresetFolder);

            var defaultName = string.IsNullOrEmpty(_previewName)
                ? "P09_BuildPreset_New"
                : $"P09_BuildPreset_{_previewName}";

            var path = EditorUtility.SaveFilePanelInProject(
                "프리셋 저장",
                defaultName,
                "asset",
                "빌드 프리셋을 저장할 위치를 선택하세요",
                PresetFolder);

            if (string.IsNullOrEmpty(path)) return;

            var dir = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir))
                PathConfig.EnsureFolderExists(dir);

            var preset = ScriptableObject.CreateInstance<BuildPreset>();
            preset.config = _config;
            preset.description = $"{_config.ActorKind} - {_previewName}";
            preset.CreatedAt = System.DateTime.Now;

            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _statusMessage = $"프리셋 저장: {path}";
            Repaint();
        }

        private void LoadPreset()
        {
            // 기본 폴더 생성 보장
            PathConfig.EnsureFolderExists(PresetFolder);

            var path = EditorUtility.OpenFilePanel(
                "프리셋 불러오기",
                PresetFolder,
                "asset");

            if (string.IsNullOrEmpty(path)) return;

            // Application.dataPath 기준으로 상대 경로 변환
            var dataPath = Application.dataPath.Replace('\\', '/');
            var normalized = path.Replace('\\', '/');
            string assetPath;
            if (normalized.StartsWith(dataPath))
                assetPath = "Assets" + normalized.Substring(dataPath.Length);
            else
                assetPath = normalized;

            var preset = AssetDatabase.LoadAssetAtPath<BuildPreset>(assetPath);
            if (preset == null || preset.config == null)
            {
                EditorUtility.DisplayDialog("오류", "프리셋을 불러올 수 없습니다.", "확인");
                return;
            }

            _config = preset.config;
            RegeneratePreviewName();
            MarkPreviewDirty();
            _statusMessage = $"프리셋 로드: {(string.IsNullOrEmpty(preset.description) ? preset.name : preset.description)}";
            Repaint();
        }
    }
}
