#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UPlayGround.Data;
using UPlayGround.Data.Cinematic;
using UPlayGround.Manager.Cinematic;

namespace UPlayGround.Editor.Cinematic
{
    /// <summary>
    /// Cinematic Stage 에셋, 무대 프리팹/씬, 궁극기 연결과 검증을 한 흐름에서 처리한다.
    /// 모든 생성/수정은 명시적인 버튼 입력에서만 수행한다.
    /// </summary>
    public sealed class CinematicStageBuilderWindow : EditorWindow
    {
        private const string CommonStylePath =
            "Assets/02.Scripts/Editor/UIToolkit/Styles/UPlayGroundEditor.uss";
        private const string StylePath =
            "Assets/02.Scripts/Editor/UIToolkit/Styles/CinematicStageBuilder.uss";
        private const string DefaultDataFolder = "Assets/10.Datas/CinematicStage";
        private const string DefaultPrefabFolder = "Assets/03.Prefabs/CinematicStage";
        private const string DefaultSceneFolder = "Assets/01.Scenes/CinematicStage";
        private const string PreloadCatalogFolder = "Assets/Resources/UPlayGround";
        private const string PreloadCatalogPath =
            PreloadCatalogFolder + "/CinematicStagePreloadCatalog.asset";

        private static readonly string[] RequiredLayers =
        {
            "UltimateStage",
            "UltimateActor",
            "UltimateVFX"
        };

        [SerializeField] private CinematicStageSO _stage;
        [SerializeField] private UltimateSequenceAsset _ultimate;

        private ObjectField _stageField;
        private ObjectField _ultimateField;
        private Label _statusPill;
        private VisualElement _stepRail;
        private VisualElement _stageInspector;
        private VisualElement _validationList;
        private VisualElement _previewSurface;
        private Image _previewImage;
        private Label _previewTitle;
        private Label _previewDescription;
        private Button _createPrefabButton;
        private Button _createSceneButton;
        private Button _connectButton;
        private Button _applyButton;
        private Button _openPrefabButton;
        private Button _pingButton;
        private IVisualElementScheduledItem _previewPoll;
        private SerializedObject _serializedStage;

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/캐릭터/궁극기/Cinematic Stage Builder",
            priority = 145)]
        public static void Open()
        {
            var window = GetWindow<CinematicStageBuilderWindow>();
            window.titleContent = new GUIContent("Cinematic Stage");
            window.minSize = new Vector2(860f, 580f);
            window.Show();
        }

        public static void Open(CinematicStageSO stage)
        {
            Open();
            var window = GetWindow<CinematicStageBuilderWindow>();
            window.SetStage(stage);
            window.Focus();
        }

        private void OnSelectionChange()
        {
            switch (Selection.activeObject)
            {
                case CinematicStageSO selectedStage:
                    SetStage(selectedStage);
                    break;
                case UltimateSequenceAsset selectedUltimate:
                    SetUltimate(selectedUltimate);
                    break;
            }
        }

        private void OnDisable()
        {
            _previewPoll?.Pause();
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            root.AddToClassList("up-editor-root");
            root.AddToClassList("up-cstage-root");
            root.AddToClassList(EditorGUIUtility.isProSkin
                ? "up-theme-dark"
                : "up-theme-light");
            LoadStyle(root, CommonStylePath);
            LoadStyle(root, StylePath);

            root.Add(BuildHeader());
            _stepRail = BuildStepRail();
            root.Add(_stepRail);

            var split = new TwoPaneSplitView(
                0,
                420f,
                TwoPaneSplitViewOrientation.Horizontal);
            split.AddToClassList("up-cstage-split");
            split.Add(BuildAuthoringPane());
            split.Add(BuildReviewPane());
            root.Add(split);
            root.Add(BuildFooter());

            BindStage();
            SetUltimate(_ultimate);
            _previewPoll = root.schedule.Execute(RefreshPreviewTexture).Every(250);
        }

        private VisualElement BuildHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("up-cstage-header");

            var titleGroup = new VisualElement();
            titleGroup.AddToClassList("up-cstage-title-group");
            titleGroup.Add(new Label("Cinematic Stage Builder")
            {
                name = "title"
            });
            titleGroup.Add(new Label(
                "실제 액터는 유지하고, 렌더 전용 클론 무대를 안전하게 저작합니다.")
            {
                name = "subtitle"
            });
            header.Add(titleGroup);

            _statusPill = new Label("준비 필요");
            _statusPill.AddToClassList("up-cstage-pill");
            header.Add(_statusPill);
            return header;
        }

        private VisualElement BuildStepRail()
        {
            var rail = new VisualElement();
            rail.AddToClassList("up-cstage-step-rail");
            return rail;
        }

        private VisualElement BuildAuthoringPane()
        {
            var pane = new VisualElement();
            pane.AddToClassList("up-cstage-authoring-pane");

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("up-cstage-scroll");

            VisualElement assetCard = CreateCard(
                "1",
                "무대 데이터",
                "CinematicStageSO를 선택하거나 새로 만듭니다.");
            _stageField = new ObjectField("Stage Asset")
            {
                objectType = typeof(CinematicStageSO),
                allowSceneObjects = false,
                value = _stage
            };
            _stageField.RegisterValueChangedCallback(
                evt => SetStage(evt.newValue as CinematicStageSO));
            assetCard.Add(_stageField);

            var assetActions = CreateButtonRow();
            assetActions.Add(CreateButton("새 에셋", CreateStageAsset, true));
            assetActions.Add(CreateButton("Project에서 찾기", PingStageAsset));
            assetCard.Add(assetActions);
            scroll.Add(assetCard);

            VisualElement sourceCard = CreateCard(
                "2",
                "무대 소스",
                "프리팹으로 빠르게 시작하거나, 독립 저작용 Additive 씬을 생성합니다.");
            _createPrefabButton = CreateButton(
                "기본 무대 프리팹 생성",
                CreateStagePrefab,
                true);
            _createSceneButton = CreateButton(
                "Additive 무대 씬 생성",
                CreateStageScene);
            sourceCard.Add(_createPrefabButton);
            sourceCard.Add(_createSceneButton);
            sourceCard.Add(CreateInlineHint(
                "Additive 씬은 발동 중 로드되지 않습니다. 부팅 단계에서 미리 로드해야 합니다."));
            scroll.Add(sourceCard);

            VisualElement settingsCard = CreateCard(
                "3",
                "연출 설정",
                "Tier, 타깃 표현, 렌더 격리와 전환을 설정합니다.");
            _stageInspector = new VisualElement();
            _stageInspector.AddToClassList("up-cstage-property-stack");
            _stageInspector.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
            {
                _serializedStage?.ApplyModifiedProperties();
                RefreshAll();
            });
            settingsCard.Add(_stageInspector);
            scroll.Add(settingsCard);

            VisualElement ultimateCard = CreateCard(
                "4",
                "궁극기 연결",
                "시퀀스에 무대를 연결하고 실제 액터 워프를 끕니다.");
            _ultimateField = new ObjectField("Ultimate Sequence")
            {
                objectType = typeof(UltimateSequenceAsset),
                allowSceneObjects = false,
                value = _ultimate
            };
            _ultimateField.RegisterValueChangedCallback(
                evt => SetUltimate(evt.newValue as UltimateSequenceAsset));
            ultimateCard.Add(_ultimateField);
            _connectButton = CreateButton(
                "선택한 궁극기에 안전 설정으로 연결",
                ConnectUltimate,
                true);
            ultimateCard.Add(_connectButton);
            ultimateCard.Add(CreateButton(
                "궁극기 시퀀스 에디터 열기",
                OpenUltimateEditor));
            scroll.Add(ultimateCard);

            pane.Add(scroll);
            return pane;
        }

        private VisualElement BuildReviewPane()
        {
            var pane = new VisualElement();
            pane.AddToClassList("up-cstage-review-pane");

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("up-cstage-scroll");

            VisualElement previewCard = CreateCard(
                null,
                "무대 미리보기",
                "프리팹 썸네일과 현재 연결 상태를 확인합니다.");
            _previewSurface = new VisualElement();
            _previewSurface.AddToClassList("up-cstage-preview");
            _previewImage = new Image { scaleMode = ScaleMode.ScaleToFit };
            _previewImage.AddToClassList("up-cstage-preview-image");
            _previewSurface.Add(_previewImage);
            _previewTitle = new Label("무대 소스 없음");
            _previewTitle.AddToClassList("up-cstage-preview-title");
            _previewSurface.Add(_previewTitle);
            _previewDescription = new Label("기본 무대 프리팹을 생성해 시작하세요.");
            _previewDescription.AddToClassList("up-cstage-preview-description");
            _previewSurface.Add(_previewDescription);
            previewCard.Add(_previewSurface);

            var previewActions = CreateButtonRow();
            _openPrefabButton = CreateButton("Prefab Mode로 열기", OpenStagePrefab);
            _pingButton = CreateButton("소스 위치 표시", PingStageSource);
            previewActions.Add(_openPrefabButton);
            previewActions.Add(_pingButton);
            previewCard.Add(previewActions);
            scroll.Add(previewCard);

            VisualElement validationCard = CreateCard(
                null,
                "실시간 검증",
                "오류는 실행 실패, 경고는 품질 또는 콘텐츠 준비 상태를 뜻합니다.");
            _validationList = new VisualElement();
            _validationList.AddToClassList("up-cstage-validation-list");
            validationCard.Add(_validationList);
            scroll.Add(validationCard);

            VisualElement guideCard = CreateCard(
                null,
                "권장 첫 수직 슬라이스",
                "작은 범위부터 검증하면 포즈·카메라·복구 문제를 빠르게 분리할 수 있습니다.");
            guideCard.Add(CreateChecklistRow("1", "CasterClone Tier로 시작"));
            guideCard.Add(CreateChecklistRow("2", "플레이어 1종 + 단순 바닥/조명"));
            guideCard.Add(CreateChecklistRow("3", "완료·인터럽트·씬 전환 복구 확인"));
            guideCard.Add(CreateChecklistRow("4", "MeshCloth 외형과 첫 발동 유라 확인"));
            scroll.Add(guideCard);

            pane.Add(scroll);
            return pane;
        }

        private VisualElement BuildFooter()
        {
            var footer = new VisualElement();
            footer.AddToClassList("up-cstage-footer");
            footer.Add(CreateButton("레이어 확인/보정", EnsureRequiredLayers));
            _applyButton = CreateButton(
                "권장 설정 적용",
                ApplyRecommendedSettings);
            footer.Add(_applyButton);
            var spacer = new VisualElement();
            spacer.AddToClassList("up-cstage-spacer");
            footer.Add(spacer);
            footer.Add(CreateButton("검증 새로고침", RefreshAll, true));
            return footer;
        }

        private void SetStage(CinematicStageSO stage)
        {
            if (_stage == stage && _serializedStage != null)
                return;
            _stage = stage;
            if (_stageField != null)
                _stageField.SetValueWithoutNotify(stage);
            BindStage();
        }

        private void SetUltimate(UltimateSequenceAsset ultimate)
        {
            _ultimate = ultimate;
            if (_ultimateField != null)
                _ultimateField.SetValueWithoutNotify(ultimate);
            RefreshAll();
        }

        private void BindStage()
        {
            _serializedStage = _stage != null ? new SerializedObject(_stage) : null;
            if (_stageInspector == null)
                return;

            _stageInspector.Clear();
            if (_serializedStage == null)
            {
                _stageInspector.Add(CreateEmptyState(
                    "연출 설정을 편집하려면 Stage Asset을 선택하세요."));
                RefreshAll();
                return;
            }

            AddPropertyGroup("등급과 폴백", "tier", "fallback");
            AddPropertyGroup(
                "무대 배치",
                "stageSceneName",
                "stagePrefab",
                "anchorOffset",
                "alignStageYawToTarget");
            AddPropertyGroup(
                "타깃 표현",
                "targetMode",
                "silhouettePrefab",
                "sizeAnchors",
                "smallHeight",
                "largeHeight",
                "giantHeight");
            AddPropertyGroup(
                "렌더와 조명",
                "stageCullingMask",
                "stageVolumeProfile",
                "hideSourceRenderers");
            AddPropertyGroup(
                "전환과 안전장치",
                "enterTransition",
                "enterTransitionDuration",
                "exitTransition",
                "exitTransitionDuration",
                "maxStageSeconds");

            _stageInspector.Bind(_serializedStage);
            RefreshAll();
        }

        private void AddPropertyGroup(string title, params string[] propertyNames)
        {
            var foldout = new Foldout { text = title, value = true };
            foldout.AddToClassList("up-cstage-foldout");
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = _serializedStage.FindProperty(propertyName);
                if (property != null)
                    foldout.Add(new PropertyField(property));
            }
            _stageInspector.Add(foldout);
        }

        private void CreateStageAsset()
        {
            EnsureFolder(DefaultDataFolder);
            string path = EditorUtility.SaveFilePanelInProject(
                "Cinematic Stage 에셋 생성",
                "CinematicStage",
                "asset",
                "연출 무대 데이터 에셋을 저장할 위치를 선택하세요.",
                DefaultDataFolder);
            if (string.IsNullOrEmpty(path))
                return;

            var asset = CreateInstance<CinematicStageSO>();
            asset.stageCullingMask = ResolveStageMask();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            SetStage(asset);
            Selection.activeObject = asset;
        }

        private void CreateStagePrefab()
        {
            if (!EnsureStageAsset())
                return;

            EnsureFolder(DefaultPrefabFolder);
            string path = EditorUtility.SaveFilePanelInProject(
                "기본 Cinematic Stage 프리팹 생성",
                $"{_stage.name}_Stage",
                "prefab",
                "무대 프리팹을 저장할 위치를 선택하세요.",
                DefaultPrefabFolder);
            if (string.IsNullOrEmpty(path))
                return;

            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject root = null;
            try
            {
                root = BuildStageHierarchy(_stage.name);
                SceneManager.MoveGameObjectToScene(root, previewScene);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null)
                    throw new InvalidOperationException("프리팹 저장에 실패했습니다.");

                Undo.RecordObject(_stage, "Cinematic Stage 프리팹 연결");
                _stage.stagePrefab = prefab;
                _stage.stageSceneName = string.Empty;
                EditorUtility.SetDirty(_stage);
                RebuildPreloadCatalog();
                AssetDatabase.SaveAssets();
                Selection.activeObject = prefab;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Cinematic Stage",
                    $"프리팹 생성에 실패했습니다.\n{exception.Message}",
                    "확인");
            }
            finally
            {
                if (root != null)
                    DestroyImmediate(root);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }

            BindStage();
        }

        private void CreateStageScene()
        {
            if (!EnsureStageAsset())
                return;

            EnsureFolder(DefaultSceneFolder);
            string path = EditorUtility.SaveFilePanelInProject(
                "Additive Cinematic Stage 씬 생성",
                $"{_stage.name}_Stage",
                "unity",
                "부팅 시 Additive로 사전 로드할 무대 씬을 저장하세요.",
                DefaultSceneFolder);
            if (string.IsNullOrEmpty(path))
                return;

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            try
            {
                GameObject root = BuildStageHierarchy(_stage.name);
                SceneManager.MoveGameObjectToScene(root, scene);
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new InvalidOperationException("씬 저장에 실패했습니다.");

                AddSceneToBuildSettings(path);
                Undo.RecordObject(_stage, "Cinematic Stage 씬 연결");
                _stage.stageSceneName = Path.GetFileNameWithoutExtension(path);
                _stage.stagePrefab = null;
                EditorUtility.SetDirty(_stage);
                RebuildPreloadCatalog();
                AssetDatabase.SaveAssets();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Cinematic Stage",
                    $"Additive 씬 생성에 실패했습니다.\n{exception.Message}",
                    "확인");
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }

            BindStage();
        }

        private static GameObject BuildStageHierarchy(string stageName)
        {
            int stageLayer = LayerMask.NameToLayer("UltimateStage");
            int stageMask = ResolveStageMask();

            var root = new GameObject($"{stageName}_StageRoot");
            SetLayer(root, stageLayer);
            CinematicStageRoot binding = root.AddComponent<CinematicStageRoot>();

            GameObject environment = CreateChild(root.transform, "Environment", stageLayer);
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "StageFloor";
            floor.transform.SetParent(environment.transform, false);
            floor.transform.localScale = new Vector3(4f, 1f, 4f);
            SetLayer(floor, stageLayer);

            GameObject actorRoot = CreateChild(root.transform, "ActorRoot", stageLayer);
            GameObject casterAnchor = CreateChild(actorRoot.transform, "CasterAnchor", stageLayer);
            GameObject lights = CreateChild(root.transform, "Lights", stageLayer);

            GameObject keyObject = CreateChild(lights.transform, "StageKeyLight", stageLayer);
            keyObject.transform.localRotation = Quaternion.Euler(45f, -35f, 0f);
            Light key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1f, 0.89f, 0.78f);
            key.intensity = 1.1f;
            key.shadows = LightShadows.Soft;
            key.cullingMask = stageMask;

            GameObject fillObject = CreateChild(lights.transform, "StageFillLight", stageLayer);
            fillObject.transform.localPosition = new Vector3(-3f, 2.5f, -2f);
            fillObject.transform.localRotation = Quaternion.Euler(25f, 35f, 0f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Spot;
            fill.color = new Color(0.4f, 0.6f, 1f);
            fill.intensity = 2f;
            fill.range = 12f;
            fill.spotAngle = 70f;
            fill.shadows = LightShadows.None;
            fill.cullingMask = stageMask;

            var serialized = new SerializedObject(binding);
            serialized.FindProperty("_actorRoot").objectReferenceValue = actorRoot.transform;
            serialized.FindProperty("_casterAnchor").objectReferenceValue = casterAnchor.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private void ConnectUltimate()
        {
            if (_stage == null || _ultimate == null)
            {
                EditorUtility.DisplayDialog(
                    "Cinematic Stage",
                    "Stage Asset과 Ultimate Sequence를 모두 선택하세요.",
                    "확인");
                return;
            }

            Undo.RecordObject(_ultimate, "궁극기 Cinematic Stage 연결");
            _ultimate.cinematicStage ??= new CinematicStageSettings();
            _ultimate.cinematicStage.enabled = true;
            _ultimate.cinematicStage.stage = _stage;
            _ultimate.placementSettings ??= new UltimatePlacementSettings();
            _ultimate.placementSettings.warpCaster = false;
            _ultimate.placementSettings.warpPrimaryTarget = false;
            _ultimate.placementSettings.restorePositionsOnFinish = false;
            EditorUtility.SetDirty(_ultimate);
            AssetDatabase.SaveAssets();
            RefreshAll();
        }

        private void ApplyRecommendedSettings()
        {
            if (!EnsureStageAsset())
                return;

            Undo.RecordObject(_stage, "Cinematic Stage 권장 설정");
            _stage.stageCullingMask = ResolveStageMask();
            _stage.hideSourceRenderers = true;
            _stage.maxStageSeconds = Mathf.Max(15f, _stage.maxStageSeconds);
            _stage.enterTransitionDuration = Mathf.Max(0.08f, _stage.enterTransitionDuration);
            _stage.exitTransitionDuration = Mathf.Max(0.12f, _stage.exitTransitionDuration);
            if (_stage.tier is CinematicStageTier.None or CinematicStageTier.CameraOnly)
                _stage.tier = CinematicStageTier.CasterClone;
            EditorUtility.SetDirty(_stage);
            RebuildPreloadCatalog();
            AssetDatabase.SaveAssets();
            BindStage();
        }

        private void EnsureRequiredLayers()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/TagManager.asset");
            if (assets.Length == 0)
                return;

            var serialized = new SerializedObject(assets[0]);
            SerializedProperty layers = serialized.FindProperty("layers");
            List<string> missingLayers = RequiredLayers
                .Where(required => FindLayer(layers, required) < 0)
                .ToList();
            var emptyIndices = new List<int>();
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                    emptyIndices.Add(i);
            }

            if (emptyIndices.Count < missingLayers.Count)
            {
                EditorUtility.DisplayDialog(
                    "레이어 공간 부족",
                    $"필수 레이어 {missingLayers.Count}개 중 빈 사용자 레이어가 " +
                    $"{emptyIndices.Count}개뿐입니다. 아무 변경도 적용하지 않았습니다.",
                    "확인");
                RefreshAll();
                return;
            }

            for (int i = 0; i < missingLayers.Count; i++)
            {
                layers.GetArrayElementAtIndex(emptyIndices[i]).stringValue = missingLayers[i];
            }

            if (missingLayers.Count > 0)
            {
                serialized.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log("[CinematicStageBuilder] 필수 Ultimate 레이어를 추가했습니다.");
            }

            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_validationList == null)
                return;

            List<ValidationItem> items = ValidateCurrentState();
            RebuildValidation(items);
            RebuildSteps(items);
            RefreshPreview();

            int errorCount = items.Count(item => item.Severity == Severity.Error);
            int warningCount = items.Count(item => item.Severity == Severity.Warning);
            _statusPill.EnableInClassList("up-cstage-pill--ok", errorCount == 0 && warningCount == 0);
            _statusPill.EnableInClassList("up-cstage-pill--warn", errorCount == 0 && warningCount > 0);
            _statusPill.EnableInClassList("up-cstage-pill--error", errorCount > 0);
            _statusPill.text = errorCount > 0
                ? $"오류 {errorCount}"
                : warningCount > 0
                    ? $"경고 {warningCount}"
                    : "사용 준비 완료";

            bool hasStage = _stage != null;
            _createPrefabButton?.SetEnabled(hasStage);
            _createSceneButton?.SetEnabled(hasStage);
            _connectButton?.SetEnabled(hasStage && _ultimate != null);
            _applyButton?.SetEnabled(hasStage);
        }

        private List<ValidationItem> ValidateCurrentState()
        {
            var items = new List<ValidationItem>();
            foreach (string layer in RequiredLayers)
            {
                if (LayerMask.NameToLayer(layer) < 0)
                    items.Add(new ValidationItem(Severity.Error, $"필수 레이어 '{layer}'가 없습니다."));
            }

            if (_stage == null)
            {
                items.Add(new ValidationItem(Severity.Error, "CinematicStageSO를 선택하세요."));
                return items;
            }

            bool hasPrefab = _stage.stagePrefab != null;
            bool hasScene = !string.IsNullOrWhiteSpace(_stage.stageSceneName);
            if (!hasPrefab && !hasScene)
                items.Add(new ValidationItem(Severity.Error, "무대 프리팹 또는 Additive 씬이 필요합니다."));
            if (hasPrefab && hasScene)
                items.Add(new ValidationItem(Severity.Warning, "씬과 프리팹이 모두 지정되어 씬이 우선됩니다."));

            if (hasPrefab)
            {
                CinematicStageRoot binding = _stage.stagePrefab.GetComponent<CinematicStageRoot>();
                if (binding == null)
                    items.Add(new ValidationItem(Severity.Error, "무대 프리팹 루트에 CinematicStageRoot가 없습니다."));
                else if (binding.ActorRoot == binding.transform
                         || binding.CasterAnchor == binding.transform)
                    items.Add(new ValidationItem(Severity.Warning, "ActorRoot 또는 CasterAnchor가 명시적으로 연결되지 않았습니다."));
            }

            if (hasScene)
            {
                string scenePath = FindScenePath(_stage.stageSceneName);
                if (string.IsNullOrEmpty(scenePath))
                    items.Add(new ValidationItem(Severity.Error, $"'{_stage.stageSceneName}' 씬 에셋을 찾을 수 없습니다."));
                else if (!EditorBuildSettings.scenes.Any(scene => scene.path == scenePath && scene.enabled))
                    items.Add(new ValidationItem(Severity.Warning, "Additive 무대 씬이 Build Settings에 활성 등록되지 않았습니다."));
                else if (!IsSceneRegisteredForPreload(_stage.stageSceneName))
                    items.Add(new ValidationItem(Severity.Error, "Additive 무대 씬이 사전 로드 카탈로그에 등록되지 않았습니다. 권장 설정 적용으로 갱신하세요."));
                else
                    items.Add(new ValidationItem(Severity.Info, "Additive 무대 씬이 부팅 사전 로드 카탈로그에 등록되어 있습니다."));
            }

            int requiredMask = ResolveStageMask();
            if ((_stage.stageCullingMask.value & requiredMask) != requiredMask)
                items.Add(new ValidationItem(Severity.Error, "Stage Culling Mask에 필수 Ultimate 레이어가 모두 포함되지 않았습니다."));
            if (!_stage.hideSourceRenderers)
                items.Add(new ValidationItem(Severity.Warning, "원본 렌더러 숨김이 꺼져 복귀 프레임에 중복 노출될 수 있습니다."));

            if (_stage.targetMode == CinematicTargetRepresentation.Silhouette
                && _stage.silhouettePrefab == null
                && _stage.tier == CinematicStageTier.BothClones)
            {
                items.Add(new ValidationItem(Severity.Error, "실루엣 타깃 표현에 Silhouette Prefab이 없습니다."));
            }
            else if (_stage.targetMode == CinematicTargetRepresentation.Silhouette
                     && _stage.silhouettePrefab == null
                     && _stage.tier == CinematicStageTier.CasterClone)
            {
                items.Add(new ValidationItem(Severity.Warning, "Silhouette Prefab이 없어 T2에서 시전자 클론만 표시됩니다."));
            }

            if (_stage.tier == CinematicStageTier.CasterClone
                && _stage.targetMode == CinematicTargetRepresentation.Clone)
            {
                items.Add(new ValidationItem(Severity.Warning, "타깃 Clone 표현은 BothClones Tier에서만 생성됩니다."));
            }

            if (_ultimate == null)
            {
                items.Add(new ValidationItem(Severity.Warning, "검증할 UltimateSequenceAsset을 선택하지 않았습니다."));
            }
            else
            {
                if (_ultimate.cinematicStage?.enabled != true
                    || _ultimate.cinematicStage.stage != _stage)
                {
                    items.Add(new ValidationItem(Severity.Warning, "선택한 궁극기에 이 무대가 연결되지 않았습니다."));
                }

                UltimatePlacementSettings placement = _ultimate.placementSettings;
                if (placement != null
                    && (placement.warpCaster
                        || placement.warpPrimaryTarget
                        || placement.restorePositionsOnFinish))
                {
                    items.Add(new ValidationItem(Severity.Error, "궁극기 배치 설정이 실제 액터 위치를 변경하도록 되어 있습니다."));
                }
            }

            if (items.All(item => item.Severity != Severity.Error))
                items.Add(new ValidationItem(Severity.Success, "핵심 런타임 진입 조건을 충족했습니다."));
            return items;
        }

        private void RebuildValidation(List<ValidationItem> items)
        {
            _validationList.Clear();
            foreach (ValidationItem item in items)
            {
                var row = new VisualElement();
                row.AddToClassList("up-cstage-validation-row");
                row.AddToClassList(item.Severity switch
                {
                    Severity.Error => "up-cstage-validation--error",
                    Severity.Warning => "up-cstage-validation--warn",
                    Severity.Success => "up-cstage-validation--ok",
                    _ => "up-cstage-validation--info"
                });
                var icon = new Label(item.Severity switch
                {
                    Severity.Error => "!",
                    Severity.Warning => "△",
                    Severity.Success => "✓",
                    _ => "i"
                });
                icon.AddToClassList("up-cstage-validation-icon");
                row.Add(icon);
                var message = new Label(item.Message);
                message.AddToClassList("up-cstage-validation-message");
                row.Add(message);
                _validationList.Add(row);
            }
        }

        private void RebuildSteps(List<ValidationItem> items)
        {
            if (_stepRail == null)
                return;
            _stepRail.Clear();
            AddStep("1", "데이터", _stage != null);
            AddStep("2", "무대", _stage != null
                                   && (_stage.stagePrefab != null
                                       || !string.IsNullOrWhiteSpace(_stage.stageSceneName)));
            AddStep("3", "렌더 격리", _stage != null
                                        && (_stage.stageCullingMask.value & ResolveStageMask())
                                        == ResolveStageMask());
            AddStep("4", "궁극기 연결", _stage != null
                                         && _ultimate?.cinematicStage?.enabled == true
                                         && _ultimate.cinematicStage.stage == _stage);
            AddStep("5", "검증", items.All(item => item.Severity != Severity.Error));
        }

        private void AddStep(string number, string label, bool complete)
        {
            var step = new VisualElement();
            step.AddToClassList("up-cstage-step");
            step.EnableInClassList("up-cstage-step--complete", complete);
            step.Add(new Label(complete ? "✓" : number)
            {
                name = "badge"
            });
            step.Add(new Label(label) { name = "label" });
            _stepRail.Add(step);
        }

        private void RefreshPreview()
        {
            if (_previewTitle == null)
                return;

            GameObject prefab = _stage != null ? _stage.stagePrefab : null;
            _previewImage.style.display = prefab != null
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            if (prefab != null)
            {
                _previewTitle.text = prefab.name;
                _previewDescription.text =
                    $"Tier {_stage.tier} · {CountRenderers(prefab)} Renderers · {CountLights(prefab)} Lights";
            }
            else if (_stage != null && !string.IsNullOrWhiteSpace(_stage.stageSceneName))
            {
                _previewTitle.text = _stage.stageSceneName;
                _previewDescription.text = "Additive 사전 로드 무대 씬";
            }
            else
            {
                _previewTitle.text = "무대 소스 없음";
                _previewDescription.text = "기본 무대 프리팹을 생성해 시작하세요.";
            }

            _openPrefabButton?.SetEnabled(prefab != null);
            _pingButton?.SetEnabled(prefab != null
                                    || _stage != null
                                    && !string.IsNullOrWhiteSpace(_stage.stageSceneName));
            RefreshPreviewTexture();
        }

        private void RefreshPreviewTexture()
        {
            if (_previewImage == null || _stage?.stagePrefab == null)
                return;
            Texture2D preview = AssetPreview.GetAssetPreview(_stage.stagePrefab)
                                ?? AssetPreview.GetMiniThumbnail(_stage.stagePrefab);
            if (preview != null)
                _previewImage.image = preview;
        }

        private void OpenStagePrefab()
        {
            if (_stage?.stagePrefab == null)
                return;
            string path = AssetDatabase.GetAssetPath(_stage.stagePrefab);
            PrefabStageUtility.OpenPrefab(path);
        }

        private void PingStageSource()
        {
            if (_stage?.stagePrefab != null)
            {
                EditorGUIUtility.PingObject(_stage.stagePrefab);
                Selection.activeObject = _stage.stagePrefab;
                return;
            }

            string path = _stage != null ? FindScenePath(_stage.stageSceneName) : null;
            SceneAsset scene = !string.IsNullOrEmpty(path)
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(path)
                : null;
            if (scene != null)
            {
                EditorGUIUtility.PingObject(scene);
                Selection.activeObject = scene;
            }
        }

        private void PingStageAsset()
        {
            if (_stage == null)
                return;
            EditorGUIUtility.PingObject(_stage);
            Selection.activeObject = _stage;
        }

        private void OpenUltimateEditor()
        {
            if (_ultimate != null)
                UPlayGround.Data.Editor.UltimateSequenceEditorWindow.Open(_ultimate);
            else
                UPlayGround.Data.Editor.UltimateSequenceEditorWindow.Open();
        }

        private bool EnsureStageAsset()
        {
            if (_stage != null)
                return true;
            EditorUtility.DisplayDialog(
                "Cinematic Stage",
                "먼저 Stage Asset을 선택하거나 생성하세요.",
                "확인");
            return false;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            int index = scenes.FindIndex(scene => scene.path == scenePath);
            if (index >= 0)
                scenes[index] = new EditorBuildSettingsScene(scenePath, true);
            else
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static int ResolveStageMask()
        {
            int mask = 0;
            foreach (string layerName in RequiredLayers)
            {
                int layer = LayerMask.NameToLayer(layerName);
                if (layer >= 0)
                    mask |= 1 << layer;
            }
            return mask;
        }

        private static int FindLayer(SerializedProperty layers, string layerName)
        {
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                    return i;
            }
            return -1;
        }

        private static void RebuildPreloadCatalog()
        {
            EnsureFolder(PreloadCatalogFolder);
            CinematicStagePreloadCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<CinematicStagePreloadCatalogSO>(
                    PreloadCatalogPath);
            if (catalog == null)
            {
                catalog = CreateInstance<CinematicStagePreloadCatalogSO>();
                AssetDatabase.CreateAsset(catalog, PreloadCatalogPath);
            }

            var sceneNames = new List<string>();
            string[] stageGuids = AssetDatabase.FindAssets("t:CinematicStageSO");
            for (int i = 0; i < stageGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(stageGuids[i]);
                CinematicStageSO stage =
                    AssetDatabase.LoadAssetAtPath<CinematicStageSO>(path);
                if (stage != null && !string.IsNullOrWhiteSpace(stage.stageSceneName))
                    sceneNames.Add(stage.stageSceneName);
            }

            Undo.RecordObject(catalog, "Cinematic Stage 사전 로드 카탈로그 갱신");
            catalog.SetSceneNames(sceneNames);
            EditorUtility.SetDirty(catalog);
        }

        private static bool IsSceneRegisteredForPreload(string sceneName)
        {
            CinematicStagePreloadCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<CinematicStagePreloadCatalogSO>(
                    PreloadCatalogPath);
            return catalog != null
                   && catalog.SceneNames.Any(
                       registered => string.Equals(
                           registered,
                           sceneName,
                           StringComparison.Ordinal));
        }

        private static string FindScenePath(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return null;
            string[] guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == sceneName)
                    return path;
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static GameObject CreateChild(Transform parent, string name, int layer)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            SetLayer(child, layer);
            return child;
        }

        private static void SetLayer(GameObject gameObject, int layer)
        {
            if (gameObject != null && layer >= 0)
                gameObject.layer = layer;
        }

        private static int CountRenderers(GameObject root) =>
            root != null ? root.GetComponentsInChildren<Renderer>(true).Length : 0;

        private static int CountLights(GameObject root) =>
            root != null ? root.GetComponentsInChildren<Light>(true).Length : 0;

        private static VisualElement CreateCard(
            string step,
            string title,
            string description)
        {
            var card = new VisualElement();
            card.AddToClassList("up-cstage-card");
            var header = new VisualElement();
            header.AddToClassList("up-cstage-card-header");
            if (!string.IsNullOrEmpty(step))
            {
                var badge = new Label(step);
                badge.AddToClassList("up-cstage-card-step");
                header.Add(badge);
            }
            var text = new VisualElement();
            text.AddToClassList("up-cstage-card-copy");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("up-cstage-card-title");
            text.Add(titleLabel);
            var descriptionLabel = new Label(description);
            descriptionLabel.AddToClassList("up-cstage-card-description");
            text.Add(descriptionLabel);
            header.Add(text);
            card.Add(header);
            return card;
        }

        private static VisualElement CreateButtonRow()
        {
            var row = new VisualElement();
            row.AddToClassList("up-cstage-button-row");
            return row;
        }

        private static Button CreateButton(
            string text,
            Action clicked,
            bool primary = false)
        {
            var button = new Button(clicked) { text = text };
            button.AddToClassList("up-cstage-button");
            if (primary)
                button.AddToClassList("up-cstage-button--primary");
            return button;
        }

        private static VisualElement CreateInlineHint(string text)
        {
            var hint = new Label(text);
            hint.AddToClassList("up-cstage-inline-hint");
            return hint;
        }

        private static VisualElement CreateEmptyState(string text)
        {
            var empty = new Label(text);
            empty.AddToClassList("up-cstage-empty");
            return empty;
        }

        private static VisualElement CreateChecklistRow(string number, string text)
        {
            var row = new VisualElement();
            row.AddToClassList("up-cstage-check-row");
            row.Add(new Label(number) { name = "number" });
            row.Add(new Label(text) { name = "text" });
            return row;
        }

        private static void LoadStyle(VisualElement root, string path)
        {
            StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (style != null)
                root.styleSheets.Add(style);
        }

        private enum Severity
        {
            Info,
            Success,
            Warning,
            Error
        }

        private readonly struct ValidationItem
        {
            public ValidationItem(Severity severity, string message)
            {
                Severity = severity;
                Message = message;
            }

            public Severity Severity { get; }
            public string Message { get; }
        }
    }
}
#endif
