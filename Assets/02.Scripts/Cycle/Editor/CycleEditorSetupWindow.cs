#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UPlayGround.Components;
using UPlayGround.Data.Cycle;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.UI;
using UPlayGround.UI;

namespace UPlayGround.Cycle.Editor
{
    /// <summary>
    /// CYCLE_EDITOR_SETUP_CHECKLIST의 반복 저작 작업을 안전하게 자동화하는 P0 설정 도우미.
    /// 위치, 아트 리소스, 전투 튜닝처럼 자동 결정할 수 없는 값은 입력 후 적용한다.
    /// </summary>
    public sealed class CycleEditorSetupWindow : EditorWindow
    {
        private const string DefaultRoot = "Assets/10.Datas/Cycle/P0";
        private const string InputActionsPath = "Assets/Resources/Input/PlayerInputActions.inputactions";

        private string _assetRoot = DefaultRoot;
        private CycleConfigSO _runConfig;
        private CycleWorldConfigSO _worldConfig;
        private CharacterWeightProfileSO _lightProfile;
        private CharacterWeightProfileSO _standardProfile;
        private CharacterWeightProfileSO _heavyProfile;
        private BossAssistDatabaseSO _assistDatabase;
        private RemainsActor _remainsPrefab;
        private CycleSpawnRole _spawnRoles = CycleSpawnRole.OuterBoss;
        private string _fixedPlayerSpawnId = string.Empty;
        private CycleWorldConfigSO _fixedPlayerSpawnSource;
        private string _sectorId = "sector_01";
        private float _safetyRadius = 10f;
        private string _keyboardBinding = "<Keyboard>/q";
        private string _gamepadBinding = string.Empty;
        private Transform _hudParent;
        private Vector2 _scroll;
        private string _lastResult = "아직 실행한 작업이 없습니다.";

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/사이클/P0 설정 도우미")]
        public static void Open()
        {
            CycleEditorSetupWindow window = GetWindow<CycleEditorSetupWindow>("사이클 P0 설정");
            window.minSize = new Vector2(560f, 680f);
            window.Show();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.HelpBox(
                "체크리스트의 반복 작업만 자동화합니다. 실행 전 씬을 저장하고, 생성 후 아래 검증과 플레이 모드 수동 검증을 진행하세요.",
                MessageType.Info);

            DrawAssetSection();
            DrawSceneSection();
            DrawSelectionSection();
            DrawProjectSection();
            DrawHudSection();
            DrawValidationSection();

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(_lastResult, MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private void DrawAssetSection()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("1. 기본 에셋", EditorStyles.boldLabel);
            _assetRoot = EditorGUILayout.TextField("생성 폴더", _assetRoot);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_assetRoot) || !_assetRoot.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                if (GUILayout.Button("공통 설정 + 무게/회복 프로필 + 어시스트 DB 생성/보정", GUILayout.Height(28f)))
                    CreateOrUpdateBaseAssets();
            }

            _runConfig = (CycleConfigSO)EditorGUILayout.ObjectField("공통 설정", _runConfig, typeof(CycleConfigSO), false);
            _worldConfig = (CycleWorldConfigSO)EditorGUILayout.ObjectField("현재 월드 설정", _worldConfig, typeof(CycleWorldConfigSO), false);
            _lightProfile = (CharacterWeightProfileSO)EditorGUILayout.ObjectField("Light 프로필", _lightProfile, typeof(CharacterWeightProfileSO), false);
            _standardProfile = (CharacterWeightProfileSO)EditorGUILayout.ObjectField("Standard 프로필", _standardProfile, typeof(CharacterWeightProfileSO), false);
            _heavyProfile = (CharacterWeightProfileSO)EditorGUILayout.ObjectField("Heavy 프로필", _heavyProfile, typeof(CharacterWeightProfileSO), false);
            _assistDatabase = (BossAssistDatabaseSO)EditorGUILayout.ObjectField("어시스트 DB", _assistDatabase, typeof(BossAssistDatabaseSO), false);
            _remainsPrefab = (RemainsActor)EditorGUILayout.ObjectField("유해 프리팹", _remainsPrefab, typeof(RemainsActor), false);
            if (GUILayout.Button("유해 프리팹 기능성 스캐폴드 생성/불러오기"))
                CreateOrLoadRemainsPrefab();
        }

        private void DrawSceneSection()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("2. 현재 씬", EditorStyles.boldLabel);
            DrawFixedPlayerSpawnField();
            if (GUILayout.Button("SceneContext.MapID로 월드 설정 생성/불러오기"))
                CreateOrLoadWorldConfig();
            if (GUILayout.Button("CycleWorldContext + BossAssistBootstrap 배치/연결"))
                SetupSceneContexts();
        }

        private void DrawFixedPlayerSpawnField()
        {
            if (_fixedPlayerSpawnSource != _worldConfig)
            {
                _fixedPlayerSpawnSource = _worldConfig;
                if (_worldConfig != null)
                    _fixedPlayerSpawnId = _worldConfig.fixedPlayerSpawnId ?? string.Empty;
            }

            _fixedPlayerSpawnId = EditorGUILayout.TextField(
                "고정 Player Spawn ID",
                _fixedPlayerSpawnId);

            using (new EditorGUI.DisabledScope(
                       _worldConfig == null
                       || string.IsNullOrWhiteSpace(_fixedPlayerSpawnId)))
            {
                if (!GUILayout.Button("현재 월드 설정에 고정 Player Spawn ID 적용"))
                    return;

                Undo.RecordObject(_worldConfig, "고정 Player Spawn ID 설정");
                _worldConfig.fixedPlayerSpawnId = _fixedPlayerSpawnId.Trim();
                EditorUtility.SetDirty(_worldConfig);
                AssetDatabase.SaveAssets();
                _lastResult = $"고정 Player Spawn ID를 '{_worldConfig.fixedPlayerSpawnId}'로 설정했습니다.";
            }
        }

        private void DrawSelectionSection()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("3. 선택 오브젝트를 스폰 지점으로 설정", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Spawn ID는 오브젝트 이름을 정규화해 만들며, 씬 내 중복이면 숫자 접미사를 붙입니다.", MessageType.None);
            _spawnRoles = (CycleSpawnRole)EditorGUILayout.EnumFlagsField("역할", _spawnRoles);
            _sectorId = EditorGUILayout.TextField("Sector ID", _sectorId);
            _safetyRadius = EditorGUILayout.FloatField("Safety Radius", _safetyRadius);
            if (GUILayout.Button("선택 항목에 CycleSpawnPoint 추가/설정"))
                SetupSelectedSpawnPoints();
            if (GUILayout.Button("선택 항목을 Respawn으로 설정 (Spawn/Respawn ID 일치)"))
                SetupSelectedRespawnPoints();
            if (GUILayout.Button("선택 항목 하나를 중앙 보스 스폰으로 설정"))
                SetupSelectedCentralBossPoint();
            if (GUILayout.Button("선택 PortalActor를 사이클 탈출 포털로 설정"))
                SetupSelectedExitPortals();
        }

        private void DrawProjectSection()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("4. 프로젝트 일괄 설정", EditorStyles.boldLabel);
            if (GUILayout.Button("씬/프리팹 캐릭터 무게 프로필 연결"))
                AssignWeightProfiles();
            if (GUILayout.Button("모든 MinimapIconConfig에 사이클 표시 활성화"))
                EnableMinimapCycleMarkers();

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("BossAssist 입력", EditorStyles.miniBoldLabel);
            _keyboardBinding = EditorGUILayout.TextField("키보드 경로", _keyboardBinding);
            _gamepadBinding = EditorGUILayout.TextField("게임패드 경로", _gamepadBinding);
            EditorGUILayout.HelpBox("입력표가 체크리스트에 없으므로 경로를 확인하세요. 빈 경로는 추가하지 않으며 기존 캐릭터 교체 바인딩은 건드리지 않습니다.", MessageType.Warning);
            if (GUILayout.Button("PlayerAction에 BossAssist 액션/바인딩 추가"))
                AddBossAssistInputAction();
        }

        private void DrawHudSection()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("5. HUD 기능성 스캐폴드", EditorStyles.boldLabel);
            _hudParent = (Transform)EditorGUILayout.ObjectField("HUD 부모", _hudParent, typeof(Transform), true);
            EditorGUILayout.HelpBox("TMP/이미지 기본 오브젝트와 참조만 만듭니다. 앵커, 크기, 폰트, 스프라이트와 최종 연출은 HUD 규칙에 맞게 조정하세요.", MessageType.None);
            using (new EditorGUI.DisabledScope(_hudParent == null))
            {
                if (GUILayout.Button("Cycle HUD + 조우 배너 + 어시스트 HUD 생성"))
                    CreateHudScaffold();
            }
        }

        private void DrawValidationSection()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("6. 검증", EditorStyles.boldLabel);
            if (GUILayout.Button("P0 현재 씬 검증 실행", GUILayout.Height(26f)))
                CycleP0Validator.ValidateCurrentScene();
            if (GUILayout.Button("체크리스트 문서 선택"))
            {
                UnityEngine.Object document = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/docs/cycle/CYCLE_EDITOR_SETUP_CHECKLIST.md");
                Selection.activeObject = document;
                EditorGUIUtility.PingObject(document);
            }
        }

        private void CreateOrUpdateBaseAssets()
        {
            EnsureAssetFolder(_assetRoot);
            _runConfig = GetOrCreateAsset<CycleConfigSO>($"{_assetRoot}/CycleConfig_P0.asset");
            SerializedObject run = new(_runConfig);
            SerializedProperty difficulties = run.FindProperty("difficultyByCycle");
            difficulties.arraySize = 3;
            SetDifficulty(difficulties.GetArrayElementAtIndex(0), 1, 1f, 1f, 1f);
            SetDifficulty(difficulties.GetArrayElementAtIndex(1), 2, 1.35f, 1.18f, 1.35f);
            SetDifficulty(difficulties.GetArrayElementAtIndex(2), 3, 1.75f, 1.38f, 1.75f);
            run.FindProperty("expLossRate").floatValue = 0.30f;
            run.FindProperty("dropUnsettledMaterials").boolValue = true;
            run.FindProperty("enableEquipmentFragmentLoss").boolValue = false;
            run.ApplyModifiedPropertiesWithoutUndo();

            VitalRecoveryPolicySO lightRecovery = GetOrCreateAsset<VitalRecoveryPolicySO>($"{_assetRoot}/VitalRecovery_Light.asset");
            VitalRecoveryPolicySO standardRecovery = GetOrCreateAsset<VitalRecoveryPolicySO>($"{_assetRoot}/VitalRecovery_Standard.asset");
            VitalRecoveryPolicySO heavyRecovery = GetOrCreateAsset<VitalRecoveryPolicySO>($"{_assetRoot}/VitalRecovery_Heavy.asset");

            _lightProfile = CreateWeightProfile("CharacterWeight_Light", CharacterWeightClass.Light, 1.15f, 1.25f, 0.70f, 0.55f, 0.45f, lightRecovery);
            _standardProfile = CreateWeightProfile("CharacterWeight_Standard", CharacterWeightClass.Standard, 1f, 1f, 1f, 1f, 0.35f, standardRecovery);
            _heavyProfile = CreateWeightProfile("CharacterWeight_Heavy", CharacterWeightClass.Heavy, 0.82f, 0.68f, 1.80f, 2.10f, 0.24f, heavyRecovery);
            _assistDatabase = GetOrCreateAsset<BossAssistDatabaseSO>($"{_assetRoot}/BossAssistDatabase_P0.asset");

            AssetDatabase.SaveAssets();
            _lastResult = "기본 P0 에셋을 생성/보정했습니다. 미정산 Item ID와 회복 확률·개수·배율은 게임 밸런스 표에 맞게 입력하세요.";
        }

        private void CreateOrLoadWorldConfig()
        {
            SceneContext sceneContext = FindFirstObjectByType<SceneContext>(FindObjectsInactive.Include);
            if (sceneContext == null || string.IsNullOrWhiteSpace(sceneContext.MapID))
            {
                _lastResult = "SceneContext가 없거나 MapID가 비어 있어 월드 설정을 만들 수 없습니다.";
                return;
            }

            EnsureAssetFolder(_assetRoot);
            string safeMapId = SanitizeId(sceneContext.MapID);
            string path = $"{_assetRoot}/CycleWorld_{safeMapId}.asset";
            CycleWorldConfigSO existingConfig =
                AssetDatabase.LoadAssetAtPath<CycleWorldConfigSO>(path);
            _worldConfig = existingConfig != null
                ? existingConfig
                : GetOrCreateAsset<CycleWorldConfigSO>(path);
            Undo.RecordObject(_worldConfig, "사이클 월드 설정 보정");
            _worldConfig.mapId = sceneContext.MapID;
            if (existingConfig == null)
                _worldConfig.fixedPlayerSpawnId = _fixedPlayerSpawnId?.Trim() ?? string.Empty;
            else
                _fixedPlayerSpawnId = _worldConfig.fixedPlayerSpawnId ?? string.Empty;
            _fixedPlayerSpawnSource = _worldConfig;
            _worldConfig.outerBossCount = 3;
            _worldConfig.maxSameSectorBossCount = 1;
            EditorUtility.SetDirty(_worldConfig);
            AssetDatabase.SaveAssets();
            _lastResult = $"{sceneContext.MapID} 월드 설정을 생성/불러왔습니다. 외곽/중앙 Boss Actor ID 풀을 입력하세요.";
        }

        private void SetupSceneContexts()
        {
            if (_runConfig == null || _worldConfig == null)
            {
                _lastResult = "공통 설정과 현재 월드 설정을 먼저 지정하세요.";
                return;
            }

            CycleWorldContext context = FindFirstObjectByType<CycleWorldContext>(FindObjectsInactive.Include);
            if (context == null)
            {
                GameObject root = new("CycleWorldContext");
                Undo.RegisterCreatedObjectUndo(root, "CycleWorldContext 생성");
                context = Undo.AddComponent<CycleWorldContext>(root);
            }
            SetObjectReference(context, "_runConfig", _runConfig);
            SetObjectReference(context, "_config", _worldConfig);
            SetObjectReference(context, "_remainsPrefab", _remainsPrefab);

            BossAssistBootstrap bootstrap = FindFirstObjectByType<BossAssistBootstrap>(FindObjectsInactive.Include);
            if (bootstrap == null)
            {
                GameObject root = new("BossAssistBootstrap");
                Undo.RegisterCreatedObjectUndo(root, "BossAssistBootstrap 생성");
                bootstrap = Undo.AddComponent<BossAssistBootstrap>(root);
            }
            SetObjectReference(bootstrap, "_database", _assistDatabase);
            MarkSceneDirty();
            _lastResult = "현재 씬의 CycleWorldContext와 BossAssistBootstrap을 배치하고 참조를 연결했습니다.";
        }

        private void SetupSelectedSpawnPoints()
        {
            int count = 0;
            HashSet<string> used = FindObjectsByType<CycleSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(value => value != null && !Selection.gameObjects.Contains(value.gameObject))
                .Select(value => value.SpawnId).Where(value => !string.IsNullOrWhiteSpace(value)).ToHashSet(StringComparer.Ordinal);

            foreach (GameObject selected in Selection.gameObjects)
            {
                CycleSpawnPoint point = selected.GetComponent<CycleSpawnPoint>() ?? Undo.AddComponent<CycleSpawnPoint>(selected);
                string id = MakeUniqueId(SanitizeId(selected.name), used);
                SerializedObject serialized = new(point);
                serialized.FindProperty("_spawnId").stringValue = id;
                serialized.FindProperty("_allowedRoles").intValue = (int)_spawnRoles;
                serialized.FindProperty("_sectorId").stringValue = _sectorId.Trim();
                serialized.FindProperty("_safetyRadius").floatValue = Mathf.Max(0f, _safetyRadius);
                serialized.ApplyModifiedProperties();
                count++;
            }
            MarkSceneDirty();
            _lastResult = $"선택 오브젝트 {count}개를 사이클 스폰 지점으로 설정했습니다.";
        }

        private void SetupSelectedRespawnPoints()
        {
            int count = 0;
            foreach (GameObject selected in Selection.gameObjects)
            {
                CycleSpawnPoint spawn = selected.GetComponent<CycleSpawnPoint>() ?? Undo.AddComponent<CycleSpawnPoint>(selected);
                string id = string.IsNullOrWhiteSpace(spawn.SpawnId) ? SanitizeId(selected.name) : spawn.SpawnId;
                SerializedObject spawnSerialized = new(spawn);
                spawnSerialized.FindProperty("_spawnId").stringValue = id;
                spawnSerialized.FindProperty("_allowedRoles").intValue |= (int)CycleSpawnRole.Respawn;
                spawnSerialized.ApplyModifiedProperties();

                CycleRespawnPoint respawn = selected.GetComponent<CycleRespawnPoint>() ?? Undo.AddComponent<CycleRespawnPoint>(selected);
                SerializedObject respawnSerialized = new(respawn);
                respawnSerialized.FindProperty("_respawnId").stringValue = id;
                respawnSerialized.FindProperty("_isActive").boolValue = true;
                respawnSerialized.ApplyModifiedProperties();
                count++;
            }
            MarkSceneDirty();
            _lastResult = $"선택 오브젝트 {count}개의 Spawn ID와 Respawn ID를 일치시켰습니다.";
        }

        private void SetupSelectedCentralBossPoint()
        {
            if (Selection.gameObjects.Length != 1)
            {
                _lastResult = "중앙 보스 위치로 사용할 오브젝트를 정확히 하나 선택하세요.";
                return;
            }

            GameObject selected = Selection.activeGameObject;
            CentralBossSpawnPoint point = selected.GetComponent<CentralBossSpawnPoint>() ?? Undo.AddComponent<CentralBossSpawnPoint>(selected);
            SerializedObject serialized = new(point);
            serialized.FindProperty("_spawnId").stringValue = "central_boss";
            serialized.ApplyModifiedProperties();
            MarkSceneDirty();
            _lastResult = "선택 오브젝트를 central_boss 스폰 지점으로 설정했습니다. 기존 중앙 스폰이 있다면 검증에서 중복이 표시됩니다.";
        }

        private void SetupSelectedExitPortals()
        {
            int count = 0;
            foreach (GameObject selected in Selection.gameObjects)
            {
                PortalActor portal = selected.GetComponent<PortalActor>();
                if (portal == null) continue;
                SerializedObject serialized = new(portal);
                serialized.FindProperty("_isCycleExitPortal").boolValue = true;
                serialized.FindProperty("_isActive").boolValue = false;
                serialized.ApplyModifiedProperties();
                count++;
            }
            MarkSceneDirty();
            _lastResult = $"PortalActor {count}개를 초기 비활성 사이클 탈출 포털로 설정했습니다. 목표 씬과 도착 ID를 확인하세요.";
        }

        private void AssignWeightProfiles()
        {
            if (_lightProfile == null || _standardProfile == null || _heavyProfile == null)
            {
                _lastResult = "Light/Standard/Heavy 프로필을 모두 지정하세요.";
                return;
            }

            int changed = 0;
            foreach (CharacterModelData model in Resources.FindObjectsOfTypeAll<CharacterModelData>())
            {
                if (EditorUtility.IsPersistent(model) || !model.gameObject.scene.IsValid()) continue;
                changed += AssignProfile(model) ? 1 : 0;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool prefabChanged = false;
                try
                {
                    foreach (CharacterModelData model in root.GetComponentsInChildren<CharacterModelData>(true))
                    {
                        if (!AssignProfile(model)) continue;
                        prefabChanged = true;
                        changed++;
                    }
                    if (prefabChanged) PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }

            AssetDatabase.SaveAssets();
            MarkSceneDirty();
            _lastResult = $"Honoka/Bokusei/H09 캐릭터 모델 {changed}개에 무게 프로필을 연결했습니다.";
        }

        private bool AssignProfile(CharacterModelData model)
        {
            CharacterWeightProfileSO profile = model.characterType switch
            {
                CharacterActorType.Honoka => _lightProfile,
                CharacterActorType.Bokusei => _standardProfile,
                CharacterActorType.H09 => _heavyProfile,
                _ => null,
            };
            if (profile == null || model.weightProfile == profile) return false;
            Undo.RecordObject(model, "캐릭터 무게 프로필 연결");
            model.weightProfile = profile;
            EditorUtility.SetDirty(model);
            return true;
        }

        private void EnableMinimapCycleMarkers()
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:MinimapIconConfigSO"))
            {
                MinimapIconConfigSO config = AssetDatabase.LoadAssetAtPath<MinimapIconConfigSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (config == null) continue;
                Undo.RecordObject(config, "사이클 미니맵 마커 활성화");
                config.showRemainsMarker = true;
                EditorUtility.SetDirty(config);
                count++;
            }
            AssetDatabase.SaveAssets();
            _lastResult = $"MinimapIconConfigSO {count}개의 유해 표시를 활성화했습니다. 스프라이트는 직접 연결하세요.";
        }

        private void AddBossAssistInputAction()
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            InputActionMap map = asset?.FindActionMap("PlayerAction", false);
            if (map == null)
            {
                _lastResult = $"{InputActionsPath}에서 PlayerAction 맵을 찾지 못했습니다.";
                return;
            }

            InputAction action = map.FindAction("BossAssist", false) ?? map.AddAction("BossAssist", InputActionType.Button);
            AddBindingIfMissing(action, _keyboardBinding);
            AddBindingIfMissing(action, _gamepadBinding);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceUpdate);
            _lastResult = "BossAssist 버튼 액션을 추가/확인했습니다. Input Actions 편집기에서 충돌 여부와 생성 C# 클래스 재생성을 확인하세요.";
        }

        private void CreateHudScaffold()
        {
            GameObject hud = CreateUiRoot("UICycleHud", _hudParent);
            UICycleHud cycleHud = Undo.AddComponent<UICycleHud>(hud);
            SetObjectReference(cycleHud, "_cycleText", CreateText("CycleText", hud.transform));
            SetObjectReference(cycleHud, "_seedText", CreateText("SeedText", hud.transform));
            SetObjectReference(cycleHud, "_elapsedText", CreateText("ElapsedText", hud.transform));

            GameObject bannerObject = CreateUiRoot("UICycleEncounterBanner", _hudParent);
            UICycleEncounterBanner banner = Undo.AddComponent<UICycleEncounterBanner>(bannerObject);
            CanvasGroup bannerGroup = Undo.AddComponent<CanvasGroup>(bannerObject);
            SetObjectReference(banner, "_title", CreateText("Title", bannerObject.transform));
            SetObjectReference(banner, "_group", bannerGroup);

            GameObject assistObject = CreateUiRoot("UIBossAssistHud", _hudParent);
            UIBossAssistHud assist = Undo.AddComponent<UIBossAssistHud>(assistObject);
            CanvasGroup assistGroup = Undo.AddComponent<CanvasGroup>(assistObject);
            Image icon = CreateImage("Icon", assistObject.transform);
            Image cooldown = CreateImage("CooldownFill", assistObject.transform);
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            SetObjectReference(assist, "_icon", icon);
            SetObjectReference(assist, "_cooldownFill", cooldown);
            SetObjectReference(assist, "_cooldownText", CreateText("CooldownText", assistObject.transform));
            SetObjectReference(assist, "_group", assistGroup);

            Selection.activeGameObject = hud;
            MarkSceneDirty();
            _lastResult = "기능성 HUD 3종을 생성하고 직렬화 참조를 연결했습니다. 레이아웃과 아트, 조우 BGM/HP바 이벤트는 수동 연결하세요.";
        }

        private void CreateOrLoadRemainsPrefab()
        {
            EnsureAssetFolder(_assetRoot);
            string path = $"{_assetRoot}/RemainsActor_P0.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing == null)
            {
                GameObject root = new("RemainsActor_P0");
                try
                {
                    root.AddComponent<RemainsActor>();
                    CapsuleCollider interaction = root.AddComponent<CapsuleCollider>();
                    interaction.isTrigger = true;
                    interaction.radius = 0.8f;
                    interaction.height = 1.2f;

                    int interactableLayer = LayerMask.NameToLayer("Interactable");
                    if (interactableLayer >= 0) root.layer = interactableLayer;

                    GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.name = "Visual_Placeholder";
                    visual.transform.SetParent(root.transform, false);
                    visual.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                    visual.transform.localScale = new Vector3(1.1f, 0.35f, 0.7f);
                    DestroyImmediate(visual.GetComponent<Collider>());
                    visual.layer = root.layer;
                    existing = PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { DestroyImmediate(root); }
            }

            _remainsPrefab = existing != null ? existing.GetComponent<RemainsActor>() : null;
            _lastResult = "유해 기능성 프리팹을 준비했습니다. Visual_Placeholder를 실제 모델로 교체하고 레이어/상호작용 표시를 플레이 모드에서 확인하세요.";
        }

        private CharacterWeightProfileSO CreateWeightProfile(string name, CharacterWeightClass weightClass,
            float move, float tempo, float damage, float breakDamage, float dodge, VitalRecoveryPolicySO recovery)
        {
            CharacterWeightProfileSO profile = GetOrCreateAsset<CharacterWeightProfileSO>($"{_assetRoot}/{name}.asset");
            profile.weightClass = weightClass;
            profile.moveSpeedMultiplier = move;
            profile.attackTempoMultiplier = tempo;
            profile.damageMultiplier = damage;
            profile.breakDamageMultiplier = breakDamage;
            profile.dodgeIFrameSeconds = dodge;
            profile.recoveryPolicy = recovery;
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void SetDifficulty(
            SerializedProperty entry,
            int index,
            float hp,
            float attack,
            float reward)
        {
            entry.FindPropertyRelative("cycleIndex").intValue = index;
            entry.FindPropertyRelative("healthMultiplier").floatValue = hp;
            entry.FindPropertyRelative("attackMultiplier").floatValue = attack;
            entry.FindPropertyRelative("rewardMultiplier").floatValue = reward;
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureAssetFolder(string path)
        {
            string normalized = path.Replace('\\', '/').TrimEnd('/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal))
                throw new ArgumentException("생성 폴더는 Assets/ 아래여야 합니다.");
            string current = "Assets";
            foreach (string part in normalized.Substring("Assets/".Length).Split('/'))
            {
                if (string.IsNullOrWhiteSpace(part)) continue;
                string next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} 직렬화 필드를 찾지 못했습니다.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static string SanitizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "spawn";
            char[] chars = value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
            string result = new(chars);
            while (result.Contains("__", StringComparison.Ordinal)) result = result.Replace("__", "_");
            result = result.Trim('_');
            return string.IsNullOrEmpty(result) ? "spawn" : result;
        }

        private static string MakeUniqueId(string basis, ISet<string> used)
        {
            string candidate = string.IsNullOrWhiteSpace(basis) ? "spawn" : basis;
            string result = candidate;
            int suffix = 2;
            while (!used.Add(result)) result = $"{candidate}_{suffix++:00}";
            return result;
        }

        private static void AddBindingIfMissing(InputAction action, string path)
        {
            if (string.IsNullOrWhiteSpace(path) || action.bindings.Any(binding => binding.path == path)) return;
            action.AddBinding(path.Trim());
        }

        private static GameObject CreateUiRoot(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(gameObject, $"{name} 생성");
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent)
        {
            GameObject gameObject = CreateUiRoot(name, parent);
            TextMeshProUGUI text = Undo.AddComponent<TextMeshProUGUI>(gameObject);
            text.text = name;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            GameObject gameObject = CreateUiRoot(name, parent);
            Image image = Undo.AddComponent<Image>(gameObject);
            image.raycastTarget = false;
            return image;
        }

        private static void MarkSceneDirty()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
