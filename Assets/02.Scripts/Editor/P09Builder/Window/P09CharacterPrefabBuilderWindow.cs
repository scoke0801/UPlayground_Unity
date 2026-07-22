using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>
    /// P09 캐릭터 프리팹 빌더. 폼과 탐색 UI는 retained-mode UI Toolkit으로 구성하고,
    /// PreviewRenderUtility 렌더 영역만 IMGUIContainer로 격리한다.
    /// </summary>
    public sealed class P09CharacterPrefabBuilderWindow : EditorWindow
    {
        private const string MenuPath = "UPlayGround/캐릭터/P09/캐릭터 프리팹 빌더";
        private const string PresetFolder = "Assets/10.Datas/Generated/BuildPresets";
        private const double PreviewDebounceSeconds = 0.2d;
        private static readonly Regex FacialHairNamePattern =
            new(@"^(?:Male|Female|Fem)_FacialHair_(\d+)$", RegexOptions.Compiled);

        [SerializeField] private CharacterBuildConfig _config = new();
        [SerializeField] private int _activeTabIndex;
        [SerializeField] private bool _showPreview = true;

        private readonly List<string> _buildLogs = new();
        private readonly List<VisualElement> _tabPanels = new();
        private readonly List<Button> _tabButtons = new();
        private P09AssetCatalog _catalog;
        private IconResolver _iconResolver;
        private PreviewSceneController _preview;
        private PrefabBuildPipeline _pipeline;
        private NameSequenceRegistry _registry;
        private SerializedObject _serializedWindow;
        private VisualElement _previewPane;
        private IMGUIContainer _previewCanvas;
        private Label _previewNameLabel;
        private Label _resolvedPathLabel;
        private Label _statusLabel;
        private Label _catalogLabel;
        private ScrollView _validationList;
        private ScrollView _logList;
        private Button _buildButton;
        private string _previewName = string.Empty;
        private string _statusMessage = "준비 완료";
        private bool _isBuilding;
        private bool _previewDirty = true;
        private double _nextPreviewRebuildTime;

        public string PreviewName => _previewName;

        [UPlayGround.EditorTools.UPlaygroundTool(MenuPath, priority = 1100)]
        public static void Open()
        {
            var window = GetWindow<P09CharacterPrefabBuilderWindow>("P09 Builder");
            window.minSize = new Vector2(980f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            minSize = new Vector2(980f, 620f);
            _config ??= new CharacterBuildConfig();
            _config.Stats ??= new StatsAssignment();
            _config.Cycle ??= new CycleBuildSettings();

            _catalog = new P09AssetCatalog();
            _catalog.Refresh();
            _iconResolver = new IconResolver();
            _iconResolver.Warmup();
            _preview = new PreviewSceneController();
            _pipeline = new PrefabBuildPipeline();
            _registry = new NameSequenceRegistry();
            _registry.Load();

            P09AssetCatalogPostprocessor.CatalogRootChanged -= OnCatalogRootChanged;
            P09AssetCatalogPostprocessor.CatalogRootChanged += OnCatalogRootChanged;
            EditorApplication.update -= HandlePreviewDebounce;
            EditorApplication.update += HandlePreviewDebounce;
            RegeneratePreviewName();
            MarkPreviewDirty();
        }

        private void OnDisable()
        {
            P09AssetCatalogPostprocessor.CatalogRootChanged -= OnCatalogRootChanged;
            EditorApplication.update -= HandlePreviewDebounce;
            _preview?.Dispose();
            _preview = null;
        }

        public void CreateGUI()
        {
            rootVisualElement.UnregisterCallback<SerializedPropertyChangeEvent>(OnSerializedPropertyChanged);
            rootVisualElement.Clear();
            _tabPanels.Clear();
            _tabButtons.Clear();
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.backgroundColor = new Color(0.12f, 0.13f, 0.15f);

            _serializedWindow = new SerializedObject(this);
            rootVisualElement.Add(BuildToolbar());
            rootVisualElement.Add(BuildWorkspace());
            rootVisualElement.Add(BuildStatusBar());
            rootVisualElement.Bind(_serializedWindow);
            rootVisualElement.RegisterCallback<SerializedPropertyChangeEvent>(OnSerializedPropertyChanged);

            SelectTab(Mathf.Clamp(_activeTabIndex, 0, _tabPanels.Count - 1));
            RefreshDerivedUi();
        }

        private VisualElement BuildToolbar()
        {
            var bar = new Toolbar { style = { minHeight = 30f } };
            var title = new Label("P09 CHARACTER BUILDER")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginLeft = 8f, marginRight = 12f }
            };
            bar.Add(title);
            _catalogLabel = new Label();
            _catalogLabel.style.color = new Color(0.55f, 0.72f, 0.86f);
            bar.Add(_catalogLabel);
            bar.Add(new ToolbarSpacer { flex = true });
            bar.Add(new ToolbarButton(SavePreset) { text = "프리셋 저장" });
            bar.Add(new ToolbarButton(LoadPreset) { text = "프리셋 불러오기" });
            bar.Add(new ToolbarButton(RefreshCatalog) { text = "카탈로그 갱신" });
            bar.Add(new ToolbarButton(RandomizeAppearance) { text = "외형 랜덤" });

            var previewToggle = new ToolbarToggle { text = "미리보기", value = _showPreview };
            previewToggle.RegisterValueChangedCallback(evt =>
            {
                _showPreview = evt.newValue;
                if (_previewPane != null)
                    _previewPane.style.display = _showPreview ? DisplayStyle.Flex : DisplayStyle.None;
                if (_showPreview) MarkPreviewDirty();
                EditorUtility.SetDirty(this);
            });
            bar.Add(previewToggle);

            _buildButton = new ToolbarButton(OnBuildClicked) { text = "▶ 빌드" };
            _buildButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _buildButton.style.color = new Color(0.55f, 1f, 0.62f);
            bar.Add(_buildButton);
            return bar;
        }

        private VisualElement BuildWorkspace()
        {
            var split = new TwoPaneSplitView(1, 470f, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1f;

            var left = new VisualElement { style = { flexGrow = 1f, minWidth = 440f } };
            var tabs = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, height = 32f, paddingLeft = 6f, paddingTop = 3f }
            };
            left.Add(tabs);

            var content = new VisualElement { style = { flexGrow = 1f } };
            left.Add(content);
            AddTab(tabs, content, "기본", BuildBasicPanel());
            AddTab(tabs, content, "외형", BuildAppearancePanel());
            AddTab(tabs, content, "무기", BuildWeaponPanel());
            AddTab(tabs, content, "전투/스탯", BuildStatsPanel());
            AddTab(tabs, content, "CYCLE", BuildCyclePanel());

            _previewPane = BuildPreviewPane();
            _previewPane.style.display = _showPreview ? DisplayStyle.Flex : DisplayStyle.None;
            split.Add(left);
            split.Add(_previewPane);
            return split;
        }

        private void AddTab(VisualElement header, VisualElement content, string title, VisualElement panel)
        {
            int index = _tabPanels.Count;
            var button = new Button(() => SelectTab(index)) { text = title };
            button.style.minWidth = title == "CYCLE" ? 82f : 68f;
            button.style.height = 26f;
            header.Add(button);
            _tabButtons.Add(button);

            panel.style.flexGrow = 1f;
            panel.style.display = DisplayStyle.None;
            content.Add(panel);
            _tabPanels.Add(panel);
        }

        private void SelectTab(int index)
        {
            if (_tabPanels.Count == 0) return;
            _activeTabIndex = Mathf.Clamp(index, 0, _tabPanels.Count - 1);
            for (int i = 0; i < _tabPanels.Count; i++)
            {
                bool selected = i == _activeTabIndex;
                _tabPanels[i].style.display = selected ? DisplayStyle.Flex : DisplayStyle.None;
                _tabButtons[i].style.backgroundColor = selected
                    ? new Color(0.18f, 0.43f, 0.62f)
                    : StyleKeyword.Null;
            }
            EditorUtility.SetDirty(this);
        }

        private VisualElement BuildBasicPanel()
        {
            var scroll = NewScroll();
            scroll.Add(Section("Actor", "생성할 런타임 Actor 계층을 선택합니다.",
                Field("_config.ActorKind", "Actor 타입"),
                Field("_config.PlayerCharacterType", "플레이어 캐릭터 슬롯")));
            scroll.Add(Section("신체", null,
                Field("_config.Sex", "성별"),
                Field("_config.BustSizeSo", "체형 (Bust)"),
                Field("_config.UseMagicaCloth", "MagicaCloth 사용"),
                Field("_config.IsRandomAppearance", "랜덤 외형 태그")));

            _previewNameLabel = new Label();
            _previewNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _resolvedPathLabel = new Label { style = { whiteSpace = WhiteSpace.Normal, color = new Color(0.55f, 0.72f, 0.86f) } };
            scroll.Add(Section("명명 및 저장", null,
                _previewNameLabel,
                Field("_config.UseManualName", "수동 이름 사용"),
                Field("_config.ManualName", "수동 이름"),
                Field("_config.SaveBaseFolder", "저장 루트"),
                _resolvedPathLabel));
            return scroll;
        }

        private VisualElement BuildAppearancePanel()
        {
            var scroll = NewScroll();
            scroll.Add(Info("P09 카탈로그 자산을 직접 연결합니다. 변경된 외형만 0.2초 지연 후 미리보기에 반영됩니다."));
            var armor = new List<VisualElement>();
            string[] labels = { "머리", "상의", "팔", "허리", "다리" };
            for (int i = 0; i < labels.Length; i++)
                armor.Add(Field($"_config.ArmorSelections._slots.Array.data[{i}]", labels[i]));
            scroll.Add(Section("방어구", null, armor.ToArray()));
            scroll.Add(Section("헤어 / 얼굴", null,
                Field("_config.HairStyleSo", "헤어 스타일"),
                Field("_config.HairColorSo", "헤어 색상"),
                Field("_config.FaceTypeSo", "얼굴"),
                Field("_config.EmotionSo", "표정"),
                Field("_config.FacialHairSo", "수염 자산"),
                Field("_config.FacialHairId", "부착 수염 ID")));
            scroll.Add(Section("색상", null,
                Field("_config.EyeColorSo", "눈 색상"),
                Field("_config.SkinColorSo", "피부 색상")));
            return scroll;
        }

        private VisualElement BuildWeaponPanel()
        {
            var scroll = NewScroll();
            scroll.Add(Section("무기 그룹", "그룹을 사용하면 개별 슬롯보다 우선합니다.",
                Field("_config.UseWeaponGroup", "무기 그룹 사용"),
                Field("_config.WeaponGroupSo", "Weapon Group")));
            scroll.Add(Section("개별 무기", null,
                Field("_config.SwordSo", "Sword"), Field("_config.SubSwordSo", "Sub Sword"),
                Field("_config.GreatSwordSo", "Great Sword"), Field("_config.ShieldSo", "Shield"),
                Field("_config.BowSo", "Bow"), Field("_config.StaffSo", "Staff"),
                Field("_config.SpearSo", "Spear"), Field("_config.DualAxeSo", "Dual Axe"),
                Field("_config.WhipSo", "Whip"), Field("_config.ShowArrows", "화살 표시")));
            return scroll;
        }

        private VisualElement BuildStatsPanel()
        {
            var scroll = NewScroll();
            scroll.Add(Info("ActorDefinitionSO와 MonsterActorProfileSO 최신 필드를 함께 동기화합니다. Profile 값이 있으면 런타임에서 Profile이 우선합니다."));
            scroll.Add(Section("몬스터 메타", null,
                Field("_config.Stats.monsterProfile", "Monster Profile"),
                Field("_config.Stats.grade", "등급"), Field("_config.Stats.level", "레벨"),
                Field("_config.Stats.monsterScaling", "Monster Scaling"),
                Field("_config.Stats.breakGaugeData", "Break Gauge")));
            scroll.Add(Section("전투 정책", null,
                Field("_config.Stats.abilitySet", "Ability Set"),
                Field("_config.Stats.combatStyle", "AI 전투 스타일"),
                Field("_config.Stats.combatDefensePolicy", "방어 정책"),
                Field("_config.Stats.combatReactionPolicy", "피격 정책"),
                Field("_config.Stats.combatElement", "전투 속성"),
                Field("_config.Stats.elementAssignmentMode", "속성 결정 방식"),
                Field("_config.Stats.elementalAdvantageMultiplier", "속성 우위 배율")));
            scroll.Add(Section("AI / Poise", null,
                Field("_config.Stats.createNewBehavior", "Behavior 새로 생성"),
                Field("_config.Stats.existingBehaviorSo", "기존 Behavior"),
                Field("_config.Stats.optimalCombatDistance", "최적 전투 거리"),
                Field("_config.Stats.createNewPoise", "Poise 새로 생성"),
                Field("_config.Stats.existingPoiseSo", "기존 Poise"),
                Field("_config.Stats.defaultMaxPoise", "최대 Poise"),
                Field("_config.Stats.defaultPoiseRecoveryDelay", "회복 지연"),
                Field("_config.Stats.defaultPoiseRecoveryRate", "초당 회복"),
                Field("_config.Stats.defaultHasHyperArmor", "Hyper Armor")));
            scroll.Add(Section("보상 / 드롭", null,
                Field("_config.Stats.dropTable", "드롭 테이블"),
                Field("_config.Stats.expReward", "경험치 보상"),
                Field("_config.Stats.goldReward", "골드 보상")));
            scroll.Add(Section("공격 스탯 베이크", null,
                Field("_config.Stats.applyLevelScaling", "레벨 스케일링"),
                Field("_config.Stats.attackPerLevel", "레벨당 공격 증가율"),
                Field("_config.Stats.applyWeaponAttackBonus", "무기 티어 보너스"),
                Field("_config.Stats.weaponAttackPerTier", "티어당 증가율"),
                Field("_config.Stats.defaultAttackDamage", "기본 공격력"),
                Field("_config.Stats.randomizeStatsOnBuild", "랜덤 배율 적용"),
                Field("_config.Stats.randomStatMin", "랜덤 최소"),
                Field("_config.Stats.randomStatMax", "랜덤 최대")));
            scroll.Add(Section("파티 캐릭터 해금", "Cycle 보스의 BossAssist 영입과는 다른 기능입니다.",
                Field("_config.Stats.recruitableOnDefeat", "처치 시 플레이어블 해금"),
                Field("_config.Stats.recruitableAs", "해금 캐릭터")));
            scroll.Add(Section("Player / NPC", null,
                Field("_config.Stats.playerAbilitySet", "Player Ability Set"),
                Field("_config.Stats.dialogueSo", "NPC 대화 데이터"),
                Field("_config.Stats.wanderRadius", "NPC 배회 반경")));
            return scroll;
        }

        private VisualElement BuildCyclePanel()
        {
            var scroll = NewScroll();
            scroll.Add(Info("Cycle 보스 후보 등록과 BossAssist 영입 데이터를 함께 생성합니다. 런타임 Handle은 월드 스폰 서비스가 자동 부착합니다."));
            scroll.Add(Section("Cycle 보스 풀", null,
                Field("_config.Cycle.isCycleBoss", "Cycle 보스로 사용"),
                Field("_config.Cycle.worldConfig", "World Config"),
                Field("_config.Cycle.registerAsOuterBoss", "외곽 보스 풀 등록"),
                Field("_config.Cycle.registerAsCentralBoss", "중앙 보스 풀 등록")));
            scroll.Add(Section("BossAssist", "미입력 Assist ID는 '<ActorId>_Assist'로 생성됩니다.",
                Field("_config.Cycle.createOrUpdateBossAssist", "정의 생성/갱신"),
                Field("_config.Cycle.assistDatabase", "Assist Database"),
                Field("_config.Cycle.assistId", "Assist ID"),
                Field("_config.Cycle.role", "역할"),
                Field("_config.Cycle.icon", "아이콘"),
                Field("_config.Cycle.assistPrefab", "전용 Assist Prefab"),
                Field("_config.Cycle.motionSet", "Motion Set"),
                Field("_config.Cycle.cooldownSeconds", "쿨다운(초)"),
                Field("_config.Cycle.maxExecutionSeconds", "최대 실행 시간"),
                Field("_config.Cycle.placementPolicy", "배치 정책"),
                Field("_config.Cycle.placementOffset", "배치 오프셋"),
                Field("_config.Cycle.requiresTarget", "타깃 필요"),
                Field("_config.Cycle.recruitableFromCentralBoss", "중앙 보스 영입 허용"),
                Field("_config.Cycle.healAmount", "기본 회복량")));
            return scroll;
        }

        private VisualElement BuildPreviewPane()
        {
            var pane = new VisualElement
            {
                style = { flexGrow = 1f, minWidth = 360f, paddingLeft = 8f, paddingRight = 8f, paddingTop = 6f }
            };
            var title = new Label("LIVE PREVIEW") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4f } };
            pane.Add(title);

            _previewCanvas = new IMGUIContainer(() =>
            {
                Rect rect = GUILayoutUtility.GetRect(320f, 1000f, 320f, 1000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                _preview?.Draw(rect);
            });
            _previewCanvas.style.flexGrow = 1f;
            _previewCanvas.style.minHeight = 280f;
            pane.Add(_previewCanvas);

            var controls = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            controls.Add(new Button(ForceRebuildPreviewNow) { text = "재빌드" });
            controls.Add(new Button(() => { _preview?.ResetView(); _previewCanvas.MarkDirtyRepaint(); }) { text = "리셋" });
            controls.Add(new Button(() => _preview?.OpenInSceneView(_config, _catalog)) { text = "SceneView" });
            pane.Add(controls);

            var fov = new Slider("FOV", 20f, 60f) { value = _preview?.CameraFov ?? 30f };
            fov.RegisterValueChangedCallback(evt => { if (_preview != null) _preview.CameraFov = evt.newValue; _previewCanvas.MarkDirtyRepaint(); });
            pane.Add(fov);
            var yOffset = new Slider("Y 위치", -1f, 1f) { value = _preview?.VerticalOffset ?? 0f };
            yOffset.RegisterValueChangedCallback(evt => { if (_preview != null) _preview.VerticalOffset = evt.newValue; _previewCanvas.MarkDirtyRepaint(); });
            pane.Add(yOffset);
            var background = new ColorField("배경") { value = _preview?.BackgroundColor ?? Color.gray };
            background.RegisterValueChangedCallback(evt => { if (_preview != null) _preview.BackgroundColor = evt.newValue; _previewCanvas.MarkDirtyRepaint(); });
            pane.Add(background);

            pane.Add(new Label("검증") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 6f } });
            _validationList = new ScrollView { style = { maxHeight = 105f, minHeight = 42f } };
            pane.Add(_validationList);
            pane.Add(new Label("빌드 로그") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 6f } });
            _logList = new ScrollView { style = { maxHeight = 100f, minHeight = 50f } };
            pane.Add(_logList);
            return pane;
        }

        private VisualElement BuildStatusBar()
        {
            var bar = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, minHeight = 25f, paddingLeft = 8f, paddingRight = 8f, paddingTop = 3f }
            };
            _statusLabel = new Label();
            bar.Add(_statusLabel);
            return bar;
        }

        private PropertyField Field(string path, string label)
        {
            SerializedProperty property = _serializedWindow.FindProperty(path);
            var field = new PropertyField(property, label);
            field.style.marginBottom = 2f;
            return field;
        }

        private static ScrollView NewScroll() => new() { style = { flexGrow = 1f, paddingLeft = 8f, paddingRight = 8f } };

        private static VisualElement Section(string title, string help, params VisualElement[] children)
        {
            var foldout = new Foldout { text = title, value = true };
            foldout.style.marginTop = 5f;
            foldout.style.paddingLeft = 5f;
            foldout.style.paddingRight = 5f;
            if (!string.IsNullOrEmpty(help)) foldout.Add(Info(help));
            foreach (VisualElement child in children) foldout.Add(child);
            return foldout;
        }

        private static HelpBox Info(string text) => new(text, HelpBoxMessageType.Info);

        private void OnSerializedPropertyChanged(SerializedPropertyChangeEvent evt)
        {
            string path = evt.changedProperty?.propertyPath ?? string.Empty;
            if (path.Contains("ActorKind") || path.Contains("ManualName") || path.Contains("UseManualName") ||
                path.Contains("SaveBaseFolder") || path.Contains("Sex") || path.Contains("IsRandomAppearance"))
                RegeneratePreviewName();

            if (path.StartsWith("_config.ArmorSelections", StringComparison.Ordinal) ||
                path.StartsWith("_config.Hair", StringComparison.Ordinal) ||
                path.StartsWith("_config.Face", StringComparison.Ordinal) ||
                path.StartsWith("_config.Emotion", StringComparison.Ordinal) ||
                path.StartsWith("_config.Eye", StringComparison.Ordinal) ||
                path.StartsWith("_config.Skin", StringComparison.Ordinal) ||
                path.StartsWith("_config.Bust", StringComparison.Ordinal) ||
                path.Contains("Weapon") || path.Contains("Sword") || path.Contains("Bow") ||
                path.Contains("Staff") || path.Contains("Spear") || path.Contains("Axe") || path.Contains("Whip") ||
                path.Contains("ShowArrows") || path.Contains("UseMagicaCloth") || path.Contains("Sex"))
                MarkPreviewDirty();

            RefreshDerivedUi();
        }

        public void RegeneratePreviewName()
        {
            if (_config == null || _registry == null) return;
            _previewName = CharacterNameGenerator.Preview(_config, _registry);
            RefreshDerivedUi();
        }

        public void MarkPreviewDirty()
        {
            _previewDirty = true;
            _nextPreviewRebuildTime = EditorApplication.timeSinceStartup + PreviewDebounceSeconds;
        }

        private void HandlePreviewDebounce()
        {
            if (!_showPreview || !_previewDirty || EditorApplication.timeSinceStartup < _nextPreviewRebuildTime)
                return;
            _previewDirty = false;
            _preview?.Rebuild(_config, _catalog);
            _previewCanvas?.MarkDirtyRepaint();
        }

        private void ForceRebuildPreviewNow()
        {
            _previewDirty = false;
            _preview?.Rebuild(_config, _catalog, true);
            _previewCanvas?.MarkDirtyRepaint();
        }

        private void RefreshDerivedUi()
        {
            if (_config == null) return;
            if (_previewNameLabel != null) _previewNameLabel.text = $"생성 이름  {_previewName}";
            string kind = CharacterNameGenerator.GetKindFolderName(_config.ActorKind);
            string folder = PathConfig.GetPrefabFolder(_config.SaveBaseFolder, kind, _previewName);
            if (_resolvedPathLabel != null) _resolvedPathLabel.text = $"경로  {folder}";
            if (_catalogLabel != null)
                _catalogLabel.text = $"Armor {_catalog?.Heads?.Count ?? 0} · Hair {_catalog?.HairStyles?.Count ?? 0} · Weapon {_catalog?.WeaponGroups?.Count ?? 0}";

            List<string> errors = CollectValidationErrors();
            if (_validationList != null)
            {
                _validationList.Clear();
                if (errors.Count == 0)
                    _validationList.Add(new Label("✓ 검증 통과") { style = { color = new Color(0.48f, 0.9f, 0.55f) } });
                else
                    foreach (string error in errors)
                        _validationList.Add(new Label($"• {error}") { style = { whiteSpace = WhiteSpace.Normal, color = new Color(1f, 0.65f, 0.35f) } });
            }
            if (_statusLabel != null) _statusLabel.text = $"{_statusMessage}    |    {_previewName}    |    오류 {errors.Count}";
        }

        private List<string> CollectValidationErrors() => _config != null
            ? new List<string>(_config.Validate())
            : new List<string> { "Config가 null입니다." };

        private void RefreshBuildLogs()
        {
            if (_logList == null) return;
            _logList.Clear();
            if (_buildLogs.Count == 0) _logList.Add(new Label("로그 없음"));
            else foreach (string log in _buildLogs) _logList.Add(new Label(log));
        }

        private void OnBuildClicked()
        {
            if (_isBuilding) return;
            List<string> errors = CollectValidationErrors();
            if (errors.Count > 0)
            {
                EditorUtility.DisplayDialog("유효성 오류", string.Join("\n", errors), "확인");
                return;
            }

            string kind = CharacterNameGenerator.GetKindFolderName(_config.ActorKind);
            string folder = PathConfig.GetPrefabFolder(_config.SaveBaseFolder, kind, _previewName);
            if (!EditorUtility.DisplayDialog("프리팹 빌드", $"'{_previewName}'을 생성합니다.\n{folder}", "빌드", "취소"))
                return;

            _isBuilding = true;
            _buildButton?.SetEnabled(false);
            _statusMessage = "빌드 중...";
            RefreshDerivedUi();
            try
            {
                BuildResult result = _pipeline.Build(_config);
                _buildLogs.Clear();
                _buildLogs.AddRange(result.Logs);
                if (result.Success)
                {
                    _statusMessage = $"완료: {result.PrefabPath}";
                    Selection.activeObject = result.Prefab;
                    if (result.Prefab != null) EditorGUIUtility.PingObject(result.Prefab);
                    _registry.Load();
                    RegeneratePreviewName();
                }
                else
                {
                    _statusMessage = $"실패: {result.ErrorMessage}";
                    if (!string.IsNullOrEmpty(result.ErrorMessage)) _buildLogs.Add(result.ErrorMessage);
                    EditorUtility.DisplayDialog("빌드 실패", result.ErrorMessage ?? "알 수 없는 오류", "확인");
                }
            }
            finally
            {
                _isBuilding = false;
                _buildButton?.SetEnabled(true);
                RefreshBuildLogs();
                RefreshDerivedUi();
            }
        }

        private void RefreshCatalog()
        {
            _catalog?.Refresh();
            _iconResolver?.ClearCache();
            _iconResolver?.Warmup();
            _statusMessage = "카탈로그 갱신 완료";
            MarkPreviewDirty();
            RefreshDerivedUi();
        }

        private void OnCatalogRootChanged()
        {
            RefreshCatalog();
            _statusMessage = "P09 카탈로그 변경 감지";
        }

        private void RandomizeAppearance()
        {
            Undo.RecordObject(this, "P09 외형 랜덤 생성");
            _config.ArmorSelections ??= new ArmorSelectionMap();
            _config.IsRandomAppearance = true;
            List<ArmorIndexPreset> presets = ArmorIndexPresetUtility.Build(_catalog);
            if (presets.Count > 0)
                ArmorIndexPresetUtility.Apply(_config.ArmorSelections, presets[UnityEngine.Random.Range(0, presets.Count)]);
            else
                foreach (BuilderArmorSlot slot in BuilderArmorSlotExtensions.All)
                    _config.ArmorSelections.Set(slot, PickOptional(GetArmorCatalog(slot)));

            _config.HairStyleSo = PickRequired(_catalog.HairStyles, _config.HairStyleSo);
            _config.HairColorSo = PickRequired(_catalog.HairColors, _config.HairColorSo);
            _config.FaceTypeSo = PickRequired(_catalog.FaceTypes, _config.FaceTypeSo);
            _config.EmotionSo = PickOptional(_catalog.Emotions);
            _config.EyeColorSo = PickRequired(_catalog.EyeColors, _config.EyeColorSo);
            _config.SkinColorSo = PickRequired(_config.Sex == BuilderSex.Male ? _catalog.SkinColorsMale : _catalog.SkinColorsFemale, _config.SkinColorSo);
            if (_config.Sex == BuilderSex.Female)
            {
                _config.BustSizeSo = PickRequired(_catalog.BustSizes, _config.BustSizeSo);
                _config.FacialHairId = 0;
                _config.FacialHairSo = null;
            }
            else
            {
                int max = GetAttachedFacialHairMaxId(_config);
                _config.FacialHairId = max > 0 ? UnityEngine.Random.Range(0, max + 1) : 0;
                _config.FacialHairSo = max > 0 ? null : PickOptional(_catalog.FacialHairs);
            }

            _serializedWindow?.Update();
            _statusMessage = "외형 랜덤 생성 완료";
            RegeneratePreviewName();
            MarkPreviewDirty();
            RefreshDerivedUi();
        }

        private List<ScriptableObject> GetArmorCatalog(BuilderArmorSlot slot) => slot switch
        {
            BuilderArmorSlot.Head => _catalog.Heads,
            BuilderArmorSlot.Chest => _catalog.Chests,
            BuilderArmorSlot.Arm => _catalog.Arms,
            BuilderArmorSlot.Waist => _catalog.Waists,
            BuilderArmorSlot.Leg => _catalog.Legs,
            _ => null,
        };

        private static ScriptableObject PickRequired(IReadOnlyList<ScriptableObject> values, ScriptableObject fallback) =>
            values == null || values.Count == 0 ? fallback : values[UnityEngine.Random.Range(0, values.Count)];

        private static ScriptableObject PickOptional(IReadOnlyList<ScriptableObject> values)
        {
            if (values == null || values.Count == 0) return null;
            int index = UnityEngine.Random.Range(0, values.Count + 1);
            return index == 0 ? null : values[index - 1];
        }

        private static int GetAttachedFacialHairMaxId(CharacterBuildConfig config)
        {
            string path = PathConfig.GetBasePrefabPath(BuilderSex.Male, config?.UseMagicaCloth == true);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return 0;
            int max = 0;
            foreach (Transform transform in prefab.GetComponentsInChildren<Transform>(true))
            {
                Match match = FacialHairNamePattern.Match(transform.name);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int id)) max = Mathf.Max(max, id);
            }
            return max;
        }

        // 기존 PreviewTab과의 소스 호환용. 새 창에서는 Preview pane을 직접 사용한다.
        public void DrawPreviewControls()
        {
            Rect rect = GUILayoutUtility.GetRect(420f, 560f, GUILayout.ExpandWidth(true));
            _preview?.Draw(rect);
        }

        private void SavePreset()
        {
            PathConfig.EnsureFolderExists(PresetFolder);
            string defaultName = string.IsNullOrEmpty(_previewName) ? "P09_BuildPreset_New" : $"P09_BuildPreset_{_previewName}";
            string path = EditorUtility.SaveFilePanelInProject("프리셋 저장", defaultName, "asset", "저장 위치를 선택하세요", PresetFolder);
            if (string.IsNullOrEmpty(path)) return;
            string directory = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(directory)) PathConfig.EnsureFolderExists(directory);
            var preset = CreateInstance<BuildPreset>();
            preset.config = _config;
            preset.description = $"{_config.ActorKind} - {_previewName}";
            preset.CreatedAt = DateTime.Now;
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            _statusMessage = $"프리셋 저장: {path}";
            RefreshDerivedUi();
        }

        private void LoadPreset()
        {
            PathConfig.EnsureFolderExists(PresetFolder);
            string path = EditorUtility.OpenFilePanel("프리셋 불러오기", PresetFolder, "asset");
            if (string.IsNullOrEmpty(path)) return;
            string dataPath = Application.dataPath.Replace('\\', '/');
            string normalized = path.Replace('\\', '/');
            string assetPath = normalized.StartsWith(dataPath, StringComparison.Ordinal)
                ? "Assets" + normalized.Substring(dataPath.Length)
                : normalized;
            BuildPreset preset = AssetDatabase.LoadAssetAtPath<BuildPreset>(assetPath);
            if (preset?.config == null)
            {
                EditorUtility.DisplayDialog("오류", "프리셋을 불러올 수 없습니다.", "확인");
                return;
            }
            _config = preset.config;
            _config.Stats ??= new StatsAssignment();
            _config.Cycle ??= new CycleBuildSettings();
            _statusMessage = $"프리셋 로드: {(string.IsNullOrEmpty(preset.description) ? preset.name : preset.description)}";
            RegeneratePreviewName();
            MarkPreviewDirty();
            CreateGUI();
        }
    }
}
