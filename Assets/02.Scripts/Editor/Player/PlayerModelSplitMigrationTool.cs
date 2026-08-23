#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Animation.Editor;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Editor.Ability;
using UPlayGround.Data.Party;
using UPlayGround.EditorTools;
using UPlayGround.Tool.Editor.Combat;

namespace UPlayGround.Editor.Player
{
    /// <summary>거대 Player 프리팹의 캐릭터 모델을 독립 Addressable 에셋으로 분리한다.</summary>
    public static class PlayerModelSplitMigrationTool
    {
        private const string ToolId =
            "UPlayGround/캐릭터/플레이어 모델 스트리밍 분리";
        private const string PlayerPrefabPath =
            "Assets/03.Prefabs/Actor/Player/Player.prefab";
        private const string ModelFolder =
            "Assets/03.Prefabs/Actor/Player/Models";
        private const string PreviewFolder =
            "Assets/03.Prefabs/Actor/Player/Preview";
        private const string SceneVariantFolder =
            "Assets/03.Prefabs/Actor/Player/SceneVariants";
        private const string DefinitionFolder =
            "Assets/10.Datas/Party/PlayerCharacters";
        private const string CatalogPath =
            "Assets/10.Datas/Party/PlayerCharacterCatalog.asset";
        private const string MotionCatalogPath =
            "Assets/10.Datas/System/MotionPreviewCatalog.asset";
        private const string ModelGroupName = "Player Models";
        private const string DefinitionGroupName = "Player Character Definitions";
        private const string CatalogAddress = "PlayerCharacterCatalog";
        private const string ModelAddressPrefix = "Player/Model/";
        private const string DefinitionAddressPrefix = "Player/Definition/";
        private const string ModelBundleLabelPrefix = "PlayerModel.";

        private sealed class CharacterSource
        {
            public CharacterActorType Type;
            public CharacterModelData Model;
            public PlayerCharacterDefinitionSO Definition;
            public string ModelPath;
            public string ModelAddress;
        }

        private sealed class ScenePlayerOverrides
        {
            public string ScenePath;
            public int PlayerIndex;
            public readonly Dictionary<CharacterActorType, string> Addresses = new();
        }

        [UPlaygroundTool(ToolId, false, 35)]
        private static void RunInteractive()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "플레이어 모델 분리",
                    "Play Mode를 종료한 뒤 실행해 주세요.",
                    "확인");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "플레이어 모델 스트리밍 분리",
                    "Player.prefab의 13개 캐릭터 모델을 독립 Addressable 프리팹으로 " +
                    "추출하고, 씬 오버라이드와 Motion Editor 프리뷰를 이관합니다.\n\n" +
                    "실행 전 현재 수정 사항을 버전 관리에서 확인했는지 확인해 주세요.",
                    "검증 후 실행",
                    "취소"))
            {
                return;
            }

            try
            {
                Execute();
                EditorUtility.DisplayDialog(
                    "플레이어 모델 분리 완료",
                    "13개 모델·정의·Motion Preview와 Addressables 구성을 갱신했습니다.",
                    "확인");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "플레이어 모델 분리 실패",
                    exception.Message,
                    "확인");
            }
        }

        /// <summary>CI와 batchmode에서 동일한 마이그레이션을 실행한다.</summary>
        public static void RunBatch()
        {
            try
            {
                Execute();
                Debug.Log("[PlayerModelSplit] BATCH_SUCCESS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>현재 분리 결과의 필수 계약만 변경 없이 검사한다.</summary>
        public static void ValidateBatch()
        {
            try
            {
                ValidateSplitResult();
                Debug.Log("[PlayerModelSplit] VALIDATION_SUCCESS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>현재 설정으로 Addressables Player Content를 빌드한다.</summary>
        public static void BuildAddressablesBatch()
        {
            try
            {
                ValidateSplitResult();
                AddressableAssetSettings.BuildPlayerContent(
                    out AddressablesPlayerBuildResult result);
                if (result == null || !string.IsNullOrEmpty(result.Error))
                {
                    throw new InvalidOperationException(
                        $"Addressables 빌드 실패: {result?.Error ?? "결과 없음"}");
                }

                AddressablesPlayerBuildResult.BundleBuildResult[]
                    playerModelBundles = result.AssetBundleBuildResults
                        .Where(bundle =>
                            bundle.SourceAssetGroup?.Name == ModelGroupName)
                        .ToArray();
                int expectedBundleCount = Enum
                    .GetValues(typeof(CharacterActorType))
                    .Cast<CharacterActorType>()
                    .Count(type => type != CharacterActorType.None);
                if (playerModelBundles.Length != expectedBundleCount)
                {
                    throw new InvalidOperationException(
                        $"Player 모델 번들은 캐릭터당 하나여야 합니다. " +
                        $"expected={expectedBundleCount}, " +
                        $"actual={playerModelBundles.Length}");
                }

                long playerModelBytes = playerModelBundles
                    .Where(bundle => File.Exists(bundle.FilePath))
                    .Sum(bundle => new FileInfo(bundle.FilePath).Length);

                Debug.Log(
                    $"[PlayerModelSplit] ADDRESSABLES_BUILD_SUCCESS " +
                    $"duration={result.Duration:F2}s, locations={result.LocationCount}, " +
                    $"playerBundles={playerModelBundles.Length}, " +
                    $"playerBytes={playerModelBytes}, " +
                    $"output={result.OutputPath}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>분리 후 Ability와 전투 데이터의 프로젝트 전체 회귀를 검사한다.</summary>
        public static void ValidateProjectDataBatch()
        {
            try
            {
                ValidateSplitResult();
                List<AbilityValidationIssue> abilityIssues =
                    AbilityDataValidator.ValidateAll();
                List<CombatValidationIssue> combatIssues =
                    CombatDataValidator.ValidateAll();
                AbilityValidationIssue[] abilityErrors = abilityIssues
                    .Where(issue =>
                        issue.Severity == AbilityValidationSeverity.Error)
                    .ToArray();
                CombatValidationIssue[] combatErrors = combatIssues
                    .Where(issue =>
                        issue.Severity == CombatValidationSeverity.Error)
                    .ToArray();

                for (int i = 0; i < abilityErrors.Length; i++)
                {
                    AbilityValidationIssue issue = abilityErrors[i];
                    Debug.LogError(
                        $"[PlayerModelSplit][Ability] {issue.Message} " +
                        $"({AssetDatabase.GetAssetPath(issue.Context)})",
                        issue.Context);
                }
                for (int i = 0; i < combatErrors.Length; i++)
                {
                    CombatValidationIssue issue = combatErrors[i];
                    Debug.LogError(
                        $"[PlayerModelSplit][Combat] {issue.Message} " +
                        $"({issue.AssetPath}/{issue.Context})");
                }

                if (abilityErrors.Length > 0 || combatErrors.Length > 0)
                {
                    throw new InvalidOperationException(
                        $"프로젝트 데이터 회귀 검증 실패: " +
                        $"Ability 오류={abilityErrors.Length}, " +
                        $"Combat 오류={combatErrors.Length}");
                }

                Debug.Log(
                    $"[PlayerModelSplit] PROJECT_DATA_VALIDATION_SUCCESS " +
                    $"abilityWarnings={abilityIssues.Count - abilityErrors.Length}, " +
                    $"combatWarnings={combatIssues.Count - combatErrors.Length}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Execute()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode에서는 실행할 수 없습니다.");

            EnsureFolder(ModelFolder);
            EnsureFolder(PreviewFolder);
            EnsureFolder(SceneVariantFolder);
            EnsureFolder(DefinitionFolder);

            GameObject playerRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                CharacterModelData[] embeddedModels = playerRoot
                    .GetComponentsInChildren<CharacterModelData>(true);
                if (embeddedModels.Length == 0)
                {
                    RebuildExistingSplitAssets();
                    ValidateSplitResult();
                    return;
                }

                List<CharacterSource> sources = ValidateAndCollectSources(
                    playerRoot, embeddedModels);
                AddressableAssetSettings settings =
                    AddressableAssetSettingsDefaultObject.Settings
                    ?? throw new InvalidOperationException(
                        "AddressableAssetSettings를 찾지 못했습니다.");
                AddressableAssetGroup modelGroup = GetOrCreateGroup(
                    settings, ModelGroupName,
                    BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);
                AddressableAssetGroup definitionGroup = GetOrCreateGroup(
                    settings, DefinitionGroupName,
                    BundledAssetGroupSchema.BundlePackingMode.PackSeparately);

                ExtractBaseModels(sources, settings, modelGroup, definitionGroup);
                BuildCatalog(sources);
                SetAddress(settings, settings.DefaultGroup, CatalogPath, CatalogAddress);

                List<ScenePlayerOverrides> sceneOverrides =
                    CaptureAndRevertSceneOverrides(sources, settings, modelGroup);
                ConvertPlayerToShell(playerRoot);
                if (!PrefabUtility.SaveAsPrefabAsset(playerRoot, PlayerPrefabPath))
                    throw new InvalidOperationException("Player 셸 프리팹 저장에 실패했습니다.");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                ApplySceneAddressOverrides(sceneOverrides);
                BuildMotionPreviewPrefabs(sources);
                UpdateMotionPreviewCatalog(sources);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ValidateSplitResult();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(playerRoot);
            }
        }

        private static List<CharacterSource> ValidateAndCollectSources(
            GameObject playerRoot,
            IReadOnlyList<CharacterModelData> embeddedModels)
        {
            int expectedCount = Enum.GetValues(typeof(CharacterActorType))
                .Cast<CharacterActorType>()
                .Count(type => type != CharacterActorType.None);
            if (embeddedModels.Count != expectedCount)
            {
                throw new InvalidOperationException(
                    $"Player 모델 수가 예상과 다릅니다. expected={expectedCount}, " +
                    $"actual={embeddedModels.Count}");
            }

            var seen = new HashSet<CharacterActorType>();
            var result = new List<CharacterSource>(embeddedModels.Count);
            for (int i = 0; i < embeddedModels.Count; i++)
            {
                CharacterModelData model = embeddedModels[i];
                if (model.characterType == CharacterActorType.None
                    || !seen.Add(model.characterType))
                {
                    throw new InvalidOperationException(
                        $"CharacterModelData 타입이 비어 있거나 중복입니다: " +
                        $"{model.name}/{model.characterType}");
                }

                ValidateModelReferencesStayInside(model, playerRoot);
                string typeName = model.characterType.ToString();
                result.Add(new CharacterSource
                {
                    Type = model.characterType,
                    Model = model,
                    ModelPath = $"{ModelFolder}/PlayerModel_{typeName}.prefab",
                    ModelAddress = $"{ModelAddressPrefix}{typeName}",
                });
            }

            return result.OrderBy(source => (int)source.Type).ToList();
        }

        private static void ValidateModelReferencesStayInside(
            CharacterModelData model,
            GameObject playerRoot)
        {
            Transform modelRoot = model.transform;
            Component[] components = modelRoot.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                    throw new InvalidOperationException(
                        $"모델에 Missing Script가 있습니다: {model.characterType}");

                var serialized = new SerializedObject(component);
                SerializedProperty iterator = serialized.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                        continue;
                    UnityEngine.Object reference = iterator.objectReferenceValue;
                    if (reference == null || EditorUtility.IsPersistent(reference))
                        continue;
                    Transform referencedTransform = reference switch
                    {
                        GameObject gameObject => gameObject.transform,
                        Component referencedComponent => referencedComponent.transform,
                        _ => null,
                    };
                    if (referencedTransform == null
                        || referencedTransform == modelRoot
                        || referencedTransform.IsChildOf(modelRoot))
                    {
                        continue;
                    }

                    if (referencedTransform == playerRoot.transform
                        || referencedTransform.IsChildOf(playerRoot.transform))
                    {
                        throw new InvalidOperationException(
                            $"모델 외부 참조를 먼저 제거해야 합니다: " +
                            $"{model.characterType}/{component.GetType().Name}." +
                            $"{iterator.propertyPath} -> {referencedTransform.name}");
                    }
                }
            }
        }

        private static void ExtractBaseModels(
            IReadOnlyList<CharacterSource> sources,
            AddressableAssetSettings settings,
            AddressableAssetGroup modelGroup,
            AddressableAssetGroup definitionGroup)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                CharacterSource source = sources[i];
                source.Definition = CreateOrUpdateDefinition(source);
                bool wasActive = source.Model.gameObject.activeSelf;
                source.Model.AssignDefinition(source.Definition);
                source.Model.gameObject.SetActive(false);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                    source.Model.gameObject, source.ModelPath, out bool success);
                source.Model.gameObject.SetActive(wasActive);
                if (!success || prefab == null)
                    throw new InvalidOperationException(
                        $"모델 프리팹 저장 실패: {source.Type}");

                SetAddress(settings, modelGroup,
                    source.ModelPath, source.ModelAddress,
                    GetModelBundleLabel(source.Type));
                string definitionPath = AssetDatabase.GetAssetPath(source.Definition);
                SetAddress(settings, definitionGroup,
                    definitionPath, GetDefinitionAddress(source.Type));
            }
        }

        private static PlayerCharacterDefinitionSO CreateOrUpdateDefinition(
            CharacterSource source)
        {
            string path = GetDefinitionPath(source.Type);
            PlayerCharacterDefinitionSO definition =
                AssetDatabase.LoadAssetAtPath<PlayerCharacterDefinitionSO>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PlayerCharacterDefinitionSO>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.characterType = source.Type;
            definition.modelAddress = source.ModelAddress;
            definition.defaultWeaponType = source.Model.defaultWeaponType;
            definition.abilitySet = source.Model.abilitySet;
            definition.abilityResourceRules = source.Model.abilityResourceRules;
            definition.weightProfile = source.Model.weightProfile;
            definition.entryAttackRange = source.Model.entryAttackRange;
            definition.requireEntryAttackLineOfSight =
                source.Model.requireLineOfSight;
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static PlayerCharacterCatalogSO BuildCatalog(
            IReadOnlyList<CharacterSource> sources)
        {
            PlayerCharacterCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<PlayerCharacterCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PlayerCharacterCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.entries.Clear();
            for (int i = 0; i < sources.Count; i++)
            {
                catalog.entries.Add(new PlayerCharacterCatalogSO.Entry
                {
                    characterType = sources[i].Type,
                    definitionAddress = GetDefinitionAddress(sources[i].Type),
                });
            }
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static List<ScenePlayerOverrides> CaptureAndRevertSceneOverrides(
            IReadOnlyList<CharacterSource> sources,
            AddressableAssetSettings settings,
            AddressableAssetGroup modelGroup)
        {
            var definitions = sources.ToDictionary(source => source.Type,
                source => source.Definition);
            var result = new List<ScenePlayerOverrides>();
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                string[] scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/01.Scenes" })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => AssetDatabase.GetDependencies(path, true)
                        .Contains(PlayerPrefabPath, StringComparer.Ordinal))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray();

                for (int sceneIndex = 0; sceneIndex < scenePaths.Length; sceneIndex++)
                {
                    string scenePath = scenePaths[sceneIndex];
                    Scene scene = EditorSceneManager.OpenScene(
                        scenePath, OpenSceneMode.Single);
                    PlayerActor[] players = scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<PlayerActor>(true))
                        .Where(IsPlayerPrefabInstance)
                        .ToArray();
                    bool sceneChanged = false;
                    for (int playerIndex = 0; playerIndex < players.Length; playerIndex++)
                    {
                        ScenePlayerOverrides captured = CapturePlayerOverrides(
                            scenePath, playerIndex, players[playerIndex],
                            definitions, settings, modelGroup);
                        if (captured.Addresses.Count == 0)
                            continue;
                        SetModelAddressOverrides(players[playerIndex], captured.Addresses);
                        result.Add(captured);
                        sceneChanged = true;
                    }

                    if (sceneChanged)
                        EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                if (previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            }

            return result;
        }

        private static ScenePlayerOverrides CapturePlayerOverrides(
            string scenePath,
            int playerIndex,
            PlayerActor player,
            IReadOnlyDictionary<CharacterActorType, PlayerCharacterDefinitionSO> definitions,
            AddressableAssetSettings settings,
            AddressableAssetGroup modelGroup)
        {
            PropertyModification[] modifications =
                PrefabUtility.GetPropertyModifications(player.gameObject)
                ?? Array.Empty<PropertyModification>();
            var modificationsByType = new Dictionary<CharacterActorType,
                List<PropertyModification>>();
            for (int i = 0; i < modifications.Length; i++)
            {
                if (!TryGetModelType(modifications[i].target, out var type))
                    continue;
                if (!modificationsByType.TryGetValue(type, out var list))
                {
                    list = new List<PropertyModification>();
                    modificationsByType.Add(type, list);
                }
                list.Add(modifications[i]);
            }
            AddStructuralModelOverrideTypes(player, modificationsByType);

            var captured = new ScenePlayerOverrides
            {
                ScenePath = scenePath,
                PlayerIndex = playerIndex,
            };
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            foreach (var pair in modificationsByType.OrderBy(value => (int)value.Key))
            {
                CharacterModelData instanceModel = player
                    .GetComponentsInChildren<CharacterModelData>(true)
                    .First(model => model.characterType == pair.Key);
                instanceModel.AssignDefinition(definitions[pair.Key]);
                string path = $"{SceneVariantFolder}/" +
                              $"PlayerModel_{pair.Key}_{sceneName}_{playerIndex}.prefab";
                SaveDetachedPrefab(
                    instanceModel.gameObject,
                    path,
                    $"씬 모델 변형 저장 실패: {scenePath}/{pair.Key}");
                string address = $"{ModelAddressPrefix}{pair.Key}/Scene/" +
                                 $"{sceneGuid}/{playerIndex}";
                SetAddress(
                    settings,
                    modelGroup,
                    path,
                    address,
                    GetModelBundleLabel(pair.Key));
                captured.Addresses.Add(pair.Key, address);

            }

            if (modificationsByType.Count > 0)
            {
                PropertyModification[] retained = modifications
                    .Where(modification =>
                        !TryGetModelType(modification.target, out _))
                    .ToArray();
                PrefabUtility.SetPropertyModifications(
                    player.gameObject,
                    retained);
                RevertStructuralModelOverrides(player);
            }

            return captured;
        }

        private static void SaveDetachedPrefab(
            GameObject source,
            string path,
            string failureMessage)
        {
            GameObject detached = UnityEngine.Object.Instantiate(source);
            detached.name = source.name;
            try
            {
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                    detached, path, out bool success);
                if (!success || prefab == null)
                    throw new InvalidOperationException(failureMessage);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(detached);
            }
        }

        private static void AddStructuralModelOverrideTypes(
            PlayerActor player,
            IDictionary<CharacterActorType, List<PropertyModification>> targets)
        {
            foreach (var added in PrefabUtility.GetAddedGameObjects(player.gameObject))
                AddStructuralType(added.instanceGameObject, targets);
            foreach (var added in PrefabUtility.GetAddedComponents(
                         player.gameObject))
                AddStructuralType(added.instanceComponent?.gameObject, targets);
            foreach (var removed in PrefabUtility.GetRemovedComponents(
                         player.gameObject))
            {
                AddStructuralType(removed.assetComponent?.gameObject, targets);
            }
        }

        private static void AddStructuralType(
            GameObject gameObject,
            IDictionary<CharacterActorType, List<PropertyModification>> targets)
        {
            CharacterModelData model = gameObject != null
                ? gameObject.GetComponentInParent<CharacterModelData>(true)
                : null;
            if (model == null || model.characterType == CharacterActorType.None
                || targets.ContainsKey(model.characterType))
            {
                return;
            }
            targets.Add(model.characterType, new List<PropertyModification>());
        }

        private static void RevertStructuralModelOverrides(PlayerActor player)
        {
            var addedComponents = PrefabUtility.GetAddedComponents(player.gameObject)
                .Where(added => added.instanceComponent != null
                                && IsUnderCharacterModel(
                                    added.instanceComponent.gameObject))
                .ToArray();
            for (int i = addedComponents.Length - 1; i >= 0; i--)
            {
                PrefabUtility.RevertAddedComponent(
                    addedComponents[i].instanceComponent,
                    InteractionMode.AutomatedAction);
            }

            var addedGameObjects = PrefabUtility.GetAddedGameObjects(player.gameObject)
                .Where(added => IsUnderCharacterModel(added.instanceGameObject))
                .OrderByDescending(added => GetTransformDepth(
                    added.instanceGameObject.transform))
                .ToArray();
            for (int i = 0; i < addedGameObjects.Length; i++)
            {
                PrefabUtility.RevertAddedGameObject(
                    addedGameObjects[i].instanceGameObject,
                    InteractionMode.AutomatedAction);
            }

            var removedComponents = PrefabUtility.GetRemovedComponents(player.gameObject)
                .Where(removed => removed.assetComponent != null
                                  && IsUnderCharacterModel(
                                      removed.assetComponent.gameObject))
                .ToArray();
            for (int i = 0; i < removedComponents.Length; i++)
            {
                PrefabUtility.RevertRemovedComponent(
                    player.gameObject,
                    removedComponents[i].assetComponent,
                    InteractionMode.AutomatedAction);
            }
        }

        private static bool IsUnderCharacterModel(GameObject gameObject) =>
            gameObject != null
            && gameObject.GetComponentInParent<CharacterModelData>(true) != null;

        private static int GetTransformDepth(Transform transform)
        {
            int depth = 0;
            while (transform != null)
            {
                depth++;
                transform = transform.parent;
            }
            return depth;
        }

        private static bool TryGetModelType(
            UnityEngine.Object target,
            out CharacterActorType type)
        {
            GameObject gameObject = target switch
            {
                GameObject value => value,
                Component value => value.gameObject,
                _ => null,
            };
            CharacterModelData model = gameObject != null
                ? gameObject.GetComponentInParent<CharacterModelData>(true)
                : null;
            type = model != null ? model.characterType : CharacterActorType.None;
            return type != CharacterActorType.None;
        }

        private static bool IsPlayerPrefabInstance(PlayerActor player)
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(
                player.gameObject);
            return source != null
                   && string.Equals(
                       AssetDatabase.GetAssetPath(source),
                       PlayerPrefabPath,
                       StringComparison.Ordinal);
        }

        private static void ConvertPlayerToShell(GameObject playerRoot)
        {
            PlayerSwapBehaviour swap =
                playerRoot.GetComponent<PlayerSwapBehaviour>()
                ?? throw new InvalidOperationException(
                    "PlayerSwapBehaviour를 찾지 못했습니다.");
            Transform modelRoot = playerRoot.transform.Find("ModelRoot");
            if (modelRoot == null)
            {
                var modelRootObject = new GameObject("ModelRoot");
                modelRoot = modelRootObject.transform;
                modelRoot.SetParent(playerRoot.transform, false);
            }

            var swapSerialized = new SerializedObject(swap);
            swapSerialized.FindProperty("_modelRoot").objectReferenceValue = modelRoot;
            swapSerialized.ApplyModifiedPropertiesWithoutUndo();

            CharacterModelData[] models = playerRoot
                .GetComponentsInChildren<CharacterModelData>(true);
            for (int i = 0; i < models.Length; i++)
                UnityEngine.Object.DestroyImmediate(models[i].gameObject);

            PlayerActor player = playerRoot.GetComponent<PlayerActor>();
            if (player != null)
            {
                var playerSerialized = new SerializedObject(player);
                SerializedProperty socketDictionary =
                    playerSerialized.FindProperty("_socketDict");
                socketDictionary?.ClearArray();
                playerSerialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ApplySceneAddressOverrides(
            IReadOnlyList<ScenePlayerOverrides> overrides)
        {
            if (overrides.Count == 0)
                return;

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var sceneGroup in overrides.GroupBy(value => value.ScenePath))
                {
                    Scene scene = EditorSceneManager.OpenScene(
                        sceneGroup.Key, OpenSceneMode.Single);
                    PlayerActor[] players = scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<PlayerActor>(true))
                        .Where(IsPlayerPrefabInstance)
                        .ToArray();
                    foreach (ScenePlayerOverrides value in sceneGroup)
                    {
                        if (value.PlayerIndex < 0 || value.PlayerIndex >= players.Length)
                            throw new InvalidOperationException(
                                $"씬 Player 인덱스가 변경되었습니다: {value.ScenePath}");
                        SetModelAddressOverrides(players[value.PlayerIndex], value.Addresses);
                    }
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                if (previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            }
        }

        private static void SetModelAddressOverrides(
            PlayerActor player,
            IReadOnlyDictionary<CharacterActorType, string> addresses)
        {
            PlayerSwapBehaviour swap =
                player.GetComponent<PlayerSwapBehaviour>();
            var serialized = new SerializedObject(swap);
            SerializedProperty entries =
                serialized.FindProperty("_modelAddressOverrides");
            entries.arraySize = addresses.Count;
            int index = 0;
            foreach (var pair in addresses.OrderBy(value => (int)value.Key))
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index++);
                entry.FindPropertyRelative("characterType").enumValueIndex =
                    (int)pair.Key;
                entry.FindPropertyRelative("modelAddress").stringValue = pair.Value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(swap);
        }

        private static void BuildMotionPreviewPrefabs(
            IReadOnlyList<CharacterSource> sources)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                CharacterSource source = sources[i];
                GameObject previewRoot =
                    PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
                try
                {
                    PlayerSwapBehaviour swap =
                        previewRoot.GetComponent<PlayerSwapBehaviour>();
                    GameObject modelPrefab =
                        AssetDatabase.LoadAssetAtPath<GameObject>(source.ModelPath);
                    GameObject instance = PrefabUtility.InstantiatePrefab(
                        modelPrefab,
                        previewRoot.scene) as GameObject;
                    if (instance == null)
                        throw new InvalidOperationException(
                            $"Motion Preview 모델 인스턴스 생성 실패: {source.Type}");
                    instance.transform.SetParent(swap.ModelRoot, false);
                    instance.name = $"PlayerModel_{source.Type}";
                    instance.SetActive(true);
                    CharacterModelData model =
                        instance.GetComponent<CharacterModelData>();
                    model.AssignDefinition(source.Definition);
                    string previewPath = GetPreviewPath(source.Type);
                    if (!PrefabUtility.SaveAsPrefabAsset(previewRoot, previewPath))
                        throw new InvalidOperationException(
                            $"Motion Preview 프리팹 저장 실패: {source.Type}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(previewRoot);
                }
            }
        }

        private static void UpdateMotionPreviewCatalog(
            IReadOnlyList<CharacterSource> sources)
        {
            MotionPreviewCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<MotionPreviewCatalogSO>(MotionCatalogPath)
                ?? throw new InvalidOperationException(
                    $"Motion Preview Catalog이 없습니다: {MotionCatalogPath}");
            catalog.subjects.RemoveAll(entry =>
                entry != null
                && (string.Equals(entry.id, "scene-player", StringComparison.Ordinal)
                    || entry.id?.StartsWith("player-", StringComparison.Ordinal) == true
                    || string.Equals(
                        AssetDatabase.GetAssetPath(entry.prefab),
                        PlayerPrefabPath,
                        StringComparison.Ordinal)));
            for (int i = 0; i < sources.Count; i++)
            {
                CharacterSource source = sources[i];
                catalog.subjects.Insert(i, new MotionPreviewCatalogSO.SubjectEntry
                {
                    id = $"player-{source.Type.ToString().ToLowerInvariant()}",
                    displayName = $"Player · {source.Type}",
                    source = MotionPreviewCatalogSO.SubjectSource.ScenePrefab,
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        GetPreviewPath(source.Type)),
                    spawnOffset = Vector3.zero,
                });
            }
            EditorUtility.SetDirty(catalog);
        }

        private static void RebuildExistingSplitAssets()
        {
            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.Settings
                ?? throw new InvalidOperationException(
                    "AddressableAssetSettings를 찾지 못했습니다.");
            AddressableAssetGroup modelGroup = GetOrCreateGroup(
                settings,
                ModelGroupName,
                BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);
            AddressableAssetGroup definitionGroup = GetOrCreateGroup(
                settings,
                DefinitionGroupName,
                BundledAssetGroupSchema.BundlePackingMode.PackSeparately);
            var sources = new List<CharacterSource>();
            foreach (CharacterActorType type in Enum.GetValues(typeof(CharacterActorType)))
            {
                if (type == CharacterActorType.None)
                    continue;
                string modelPath = GetModelPath(type);
                PlayerCharacterDefinitionSO definition =
                    AssetDatabase.LoadAssetAtPath<PlayerCharacterDefinitionSO>(
                        GetDefinitionPath(type));
                if (definition == null
                    || AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) == null)
                {
                    throw new InvalidOperationException(
                        $"기존 분리 에셋이 불완전합니다: {type}");
                }
                sources.Add(new CharacterSource
                {
                    Type = type,
                    Definition = definition,
                    ModelPath = modelPath,
                    ModelAddress = definition.modelAddress,
                });
                SetAddress(
                    settings,
                    modelGroup,
                    modelPath,
                    definition.modelAddress,
                    GetModelBundleLabel(type));
                SetAddress(
                    settings,
                    definitionGroup,
                    GetDefinitionPath(type),
                    GetDefinitionAddress(type));
            }

            string[] variantPaths = AssetDatabase.FindAssets(
                    "t:Prefab", new[] { SceneVariantFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            for (int i = 0; i < variantPaths.Length; i++)
            {
                string variantPath = variantPaths[i];
                GameObject variant =
                    AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
                CharacterModelData model =
                    variant?.GetComponent<CharacterModelData>();
                AddressableAssetEntry entry = settings.FindAssetEntry(
                    AssetDatabase.AssetPathToGUID(variantPath));
                if (model == null || entry == null
                    || string.IsNullOrWhiteSpace(entry.address))
                {
                    throw new InvalidOperationException(
                        $"씬 모델 Variant Addressable 정보가 없습니다: {variantPath}");
                }

                SetAddress(
                    settings,
                    modelGroup,
                    variantPath,
                    entry.address,
                    GetModelBundleLabel(model.characterType));
            }

            SetAddress(
                settings,
                settings.DefaultGroup,
                CatalogPath,
                CatalogAddress);
            ReserializePlayerModelPrefabs(ModelFolder);
            ReserializePlayerModelPrefabs(SceneVariantFolder);
            BuildMotionPreviewPrefabs(sources);
            UpdateMotionPreviewCatalog(sources);
            AssetDatabase.SaveAssets();
        }

        private static void ReserializePlayerModelPrefabs(string folder)
        {
            string[] prefabPaths = AssetDatabase.FindAssets(
                    "t:Prefab", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            for (int i = 0; i < prefabPaths.Length; i++)
            {
                string path = prefabPaths[i];
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    CharacterModelData model =
                        root.GetComponent<CharacterModelData>();
                    if (model == null || model.Definition == null)
                        throw new InvalidOperationException(
                            $"정의가 연결되지 않은 Player 모델 프리팹입니다: {path}");
                    if (!PrefabUtility.SaveAsPrefabAsset(root, path))
                        throw new InvalidOperationException(
                            $"Player 모델 프리팹 재직렬화 실패: {path}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void ValidateSplitResult()
        {
            GameObject playerRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                ValidateNoMissingScripts(playerRoot, PlayerPrefabPath);
                if (playerRoot.GetComponentsInChildren<CharacterModelData>(true).Length != 0)
                    throw new InvalidOperationException(
                        "Player 셸에 CharacterModelData가 남아 있습니다.");
                PlayerSwapBehaviour swap =
                    playerRoot.GetComponent<PlayerSwapBehaviour>();
                if (swap == null || swap.ModelRoot == null
                    || swap.ModelRoot.name != "ModelRoot")
                {
                    throw new InvalidOperationException(
                        "Player 셸의 ModelRoot 연결이 유효하지 않습니다.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(playerRoot);
            }

            PlayerCharacterCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<PlayerCharacterCatalogSO>(CatalogPath)
                ?? throw new InvalidOperationException("플레이어 카탈로그가 없습니다.");
            int expected = Enum.GetValues(typeof(CharacterActorType))
                .Cast<CharacterActorType>()
                .Count(type => type != CharacterActorType.None);
            if (catalog.entries.Count != expected)
                throw new InvalidOperationException(
                    $"플레이어 카탈로그 수가 잘못되었습니다: {catalog.entries.Count}");

            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.Settings
                ?? throw new InvalidOperationException(
                    "Addressables Settings가 없습니다.");
            AddressableAssetGroup modelGroup = settings?.FindGroup(ModelGroupName);
            if (modelGroup?.GetSchema<BundledAssetGroupSchema>()?.BundleMode
                != BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel)
            {
                throw new InvalidOperationException(
                    "Player Models 그룹이 캐릭터 Label 단위 패킹이 아닙니다.");
            }
            AddressableAssetGroup definitionGroup =
                settings.FindGroup(DefinitionGroupName);
            if (definitionGroup?.GetSchema<BundledAssetGroupSchema>()?.BundleMode
                != BundledAssetGroupSchema.BundlePackingMode.PackSeparately)
            {
                throw new InvalidOperationException(
                    "Player Character Definitions 그룹이 Pack Separately가 아닙니다.");
            }

            ValidateAddressableEntry(
                settings,
                CatalogPath,
                CatalogAddress,
                null);

            var catalogTypes = new HashSet<CharacterActorType>();
            for (int i = 0; i < catalog.entries.Count; i++)
            {
                PlayerCharacterCatalogSO.Entry entry = catalog.entries[i];
                if (entry == null
                    || entry.characterType == CharacterActorType.None
                    || !catalogTypes.Add(entry.characterType)
                    || entry.definitionAddress !=
                       GetDefinitionAddress(entry.characterType))
                {
                    throw new InvalidOperationException(
                        $"플레이어 카탈로그 항목이 비어 있거나 중복입니다: index={i}");
                }
            }

            MotionPreviewCatalogSO motionCatalog =
                AssetDatabase.LoadAssetAtPath<MotionPreviewCatalogSO>(MotionCatalogPath)
                ?? throw new InvalidOperationException(
                    $"Motion Preview Catalog이 없습니다: {MotionCatalogPath}");

            foreach (CharacterActorType type in Enum.GetValues(typeof(CharacterActorType)))
            {
                if (type == CharacterActorType.None)
                    continue;
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                    GetModelPath(type));
                PlayerCharacterDefinitionSO definition =
                    AssetDatabase.LoadAssetAtPath<PlayerCharacterDefinitionSO>(
                        GetDefinitionPath(type));
                GameObject preview = AssetDatabase.LoadAssetAtPath<GameObject>(
                    GetPreviewPath(type));
                CharacterModelData modelData =
                    model?.GetComponent<CharacterModelData>();
                if (modelData == null || definition == null || preview == null
                    || model.GetComponentsInChildren<CharacterModelData>(true).Length != 1
                    || preview.GetComponentsInChildren<CharacterModelData>(true).Length != 1
                    || modelData.Definition != definition
                    || definition.characterType != type
                    || definition.modelAddress != $"{ModelAddressPrefix}{type}")
                {
                    throw new InvalidOperationException(
                        $"플레이어 분리 에셋 계약 위반: {type}");
                }

                ValidateNoMissingScripts(model, GetModelPath(type));
                ValidateNoMissingScripts(preview, GetPreviewPath(type));
                ValidateAddressableEntry(
                    settings,
                    GetModelPath(type),
                    $"{ModelAddressPrefix}{type}",
                    modelGroup,
                    GetModelBundleLabel(type));
                ValidateAddressableEntry(
                    settings,
                    GetDefinitionPath(type),
                    GetDefinitionAddress(type),
                    definitionGroup);

                string previewId =
                    $"player-{type.ToString().ToLowerInvariant()}";
                int previewCount = motionCatalog.subjects.Count(entry =>
                    entry != null
                    && entry.id == previewId
                    && entry.source ==
                       MotionPreviewCatalogSO.SubjectSource.ScenePrefab
                    && AssetDatabase.GetAssetPath(entry.prefab) ==
                       GetPreviewPath(type));
                if (previewCount != 1)
                {
                    throw new InvalidOperationException(
                        $"Motion Preview 항목이 없거나 중복입니다: {type}");
                }
            }

            string[] variantPaths = AssetDatabase.FindAssets(
                    "t:Prefab", new[] { SceneVariantFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            for (int i = 0; i < variantPaths.Length; i++)
            {
                string variantPath = variantPaths[i];
                GameObject variant =
                    AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
                ValidateNoMissingScripts(variant, variantPath);
                AddressableAssetEntry entry = settings.FindAssetEntry(
                    AssetDatabase.AssetPathToGUID(variantPath));
                CharacterModelData model =
                    variant?.GetComponent<CharacterModelData>();
                if (entry == null
                    || entry.parentGroup != modelGroup
                    || model == null
                    || !entry.labels.Contains(
                        GetModelBundleLabel(model.characterType))
                    || !entry.address.StartsWith(
                        $"{ModelAddressPrefix}",
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"씬 모델 Variant Addressable 계약 위반: {variantPath}");
                }
            }
        }

        private static void ValidateAddressableEntry(
            AddressableAssetSettings settings,
            string assetPath,
            string expectedAddress,
            AddressableAssetGroup expectedGroup,
            string expectedLabel = null)
        {
            AddressableAssetEntry entry = settings.FindAssetEntry(
                AssetDatabase.AssetPathToGUID(assetPath));
            if (entry == null
                || entry.address != expectedAddress
                || expectedGroup != null && entry.parentGroup != expectedGroup
                || expectedLabel != null && !entry.labels.Contains(expectedLabel))
            {
                throw new InvalidOperationException(
                    $"Addressable 항목 계약 위반: {assetPath} -> {expectedAddress}");
            }
        }

        private static void ValidateNoMissingScripts(
            GameObject root,
            string assetPath)
        {
            if (root == null)
                throw new InvalidOperationException($"프리팹이 없습니다: {assetPath}");

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                GameObject target = transforms[i].gameObject;
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target) > 0)
                {
                    throw new InvalidOperationException(
                        $"Missing Script가 있습니다: {assetPath}/{target.name}");
                }
            }
        }

        private static AddressableAssetGroup GetOrCreateGroup(
            AddressableAssetSettings settings,
            string groupName,
            BundledAssetGroupSchema.BundlePackingMode bundleMode)
        {
            AddressableAssetGroup group = settings.FindGroup(groupName)
                ?? settings.CreateGroup(
                    groupName, false, false, true, null,
                    typeof(ContentUpdateGroupSchema),
                    typeof(BundledAssetGroupSchema));
            BundledAssetGroupSchema schema =
                group.GetSchema<BundledAssetGroupSchema>()
                ?? group.AddSchema<BundledAssetGroupSchema>();
            schema.BundleMode = bundleMode;
            EditorUtility.SetDirty(schema);
            return group;
        }

        private static void SetAddress(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string assetPath,
            string address,
            string bundleLabel = null)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException(
                    $"Addressable 대상 GUID가 없습니다: {assetPath}");
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(
                guid, group, false, false);
            entry.SetAddress(address, false);
            if (!string.IsNullOrWhiteSpace(bundleLabel))
            {
                string[] staleLabels = entry.labels
                    .Where(label =>
                        label.StartsWith(
                            ModelBundleLabelPrefix,
                            StringComparison.Ordinal)
                        && label != bundleLabel)
                    .ToArray();
                for (int i = 0; i < staleLabels.Length; i++)
                    entry.SetLabel(staleLabels[i], false, false, false);
                entry.SetLabel(bundleLabel, true, true, false);
            }
        }

        private static string GetModelPath(CharacterActorType type) =>
            $"{ModelFolder}/PlayerModel_{type}.prefab";

        private static string GetPreviewPath(CharacterActorType type) =>
            $"{PreviewFolder}/PlayerPreview_{type}.prefab";

        private static string GetDefinitionPath(CharacterActorType type) =>
            $"{DefinitionFolder}/PlayerCharacterDefinition_{type}.asset";

        private static string GetDefinitionAddress(CharacterActorType type) =>
            $"{DefinitionAddressPrefix}{type}";

        private static string GetModelBundleLabel(CharacterActorType type) =>
            $"{ModelBundleLabelPrefix}{type}";

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
    }
}
#endif
