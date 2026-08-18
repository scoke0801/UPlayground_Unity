#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.World;
using UPlayGround.Group;
using UPlayGround.Manager.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class WorldPlacementBakeUtility
    {
        private const string OutputFolder = "Assets/10.Datas/Map/Placement";
        private const string LoaderRootName = "RuntimePlacementLoader";

        /// <summary>레코드를 목록·씬 마커·확인 다이얼로그에서 같은 이름으로 부르기 위한 표시명.</summary>
        public static string GetRecordDisplayName(WorldPlacementRecord record)
        {
            if (record == null)
                return "(빈 레코드)";

            if (!string.IsNullOrEmpty(record.actorId))
                return string.IsNullOrEmpty(record.groupName) ? record.actorId : $"{record.actorId} @{record.groupName}";

            if (record.prefab != null)
                return record.prefab.name;

            return string.IsNullOrEmpty(record.prefabId) ? "(이름 없음)" : record.prefabId;
        }

        /// <summary>
        /// Bake를 수행하고 결과 PlacementData 에셋을 반환한다. 취소/대상 없음이면 null.
        /// 씬 로더가 이미 데이터를 참조 중이면 새 에셋을 만들지 않고 해당 에셋에 병합한다
        /// (같은 placementGuid는 제자리 갱신, 새 GUID는 추가 — 기존 레코드는 유실되지 않는다).
        /// </summary>
        public static WorldPlacementDataSO BakeOpenSceneRuntimeData()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("RuntimeData Bake", "활성 씬이 유효하지 않습니다.", "확인");
                return null;
            }

            var targets = CollectRuntimeDataTargets(scene);
            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("RuntimeData Bake", "활성 씬에 RuntimeData Bake 대상이 없습니다.", "확인");
                return null;
            }

            WorldPlacementDataSO existingData = FindExistingPlacementData(scene);

            string confirmMessage = existingData != null
                ? $"{targets.Count}개 RuntimeData 배치를 기존 데이터 '{existingData.name}'(레코드 {existingData.Records.Count}개)에 병합하고 씬 오브젝트를 제거합니다.\n기존 레코드는 유지됩니다. 계속하시겠습니까?"
                : $"{targets.Count}개 RuntimeData 배치를 새 PlacementData로 저장하고 씬 오브젝트를 제거합니다.\n계속하시겠습니까?";

            if (!EditorUtility.DisplayDialog("RuntimeData Bake", confirmMessage, "Bake", "취소"))
                return null;

            // ActorDefinition 배치는 actorId만 기록해 프리팹 직접 참조(씬 데이터 중복 포함)를 끊는다.
            ActorDatabase actorDatabase = LoadActorDatabaseAsset();
            var newRecords = targets
                .Select(metadata => CreateRecord(metadata, actorDatabase))
                .Where(record => record != null)
                .ToList();

            WorldPlacementDataSO placementData;
            string resultMessage;
            if (existingData != null)
            {
                placementData = existingData;
                MergeRecords(placementData, newRecords, out int updatedCount, out int addedCount);
                EditorUtility.SetDirty(placementData);
                AssetDatabase.SaveAssets();
                resultMessage =
                    $"'{placementData.name}'에 병합 완료 — 갱신 {updatedCount}개, 추가 {addedCount}개 (총 {placementData.Records.Count}개).";
            }
            else
            {
                EnsureFolder(OutputFolder);
                string sceneName = string.IsNullOrWhiteSpace(scene.name) ? "UntitledScene" : scene.name;
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{OutputFolder}/PlacementData_{sceneName}.asset");
                placementData = ScriptableObject.CreateInstance<WorldPlacementDataSO>();
                placementData.SetRecords(newRecords);
                AssetDatabase.CreateAsset(placementData, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                resultMessage = $"{newRecords.Count}개 배치를 저장했습니다.\n{assetPath}";
            }

            EnsureRuntimeLoader(scene, placementData);

            foreach (var metadata in targets)
            {
                if (metadata != null)
                    Undo.DestroyObjectImmediate(metadata.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorUtility.DisplayDialog(
                "RuntimeData Bake 완료",
                $"{resultMessage}\n\n씬 저장 후 PlayMode에서 RuntimePlacementLoader가 생성합니다.",
                "확인");
            return placementData;
        }

        /// <summary>
        /// 복원 오브젝트의 부모를 결정한다. 씬에 해당 그룹이 있으면 그룹 하위로 —
        /// 재Bake 시 GetComponentInParent로 같은 그룹이 다시 기록되도록 왕복을 보존한다.
        /// </summary>
        /// <remarks>
        /// GUID 매칭이 1순위다. 이름만으로 찾으면 동명 그룹이 있을 때 다른 그룹에 잘못 복원되며,
        /// 그룹 프리셋으로 그룹 수가 늘어날수록 충돌 확률이 올라간다.
        /// </remarks>
        private static Transform ResolveRestoreParent(WorldPlacementRecord record, Transform fallback)
        {
            if (string.IsNullOrEmpty(record.groupGuid) && string.IsNullOrEmpty(record.groupName))
                return fallback;

            var groups = UnityEngine.Object.FindObjectsByType<MonsterGroupController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (!string.IsNullOrEmpty(record.groupGuid))
            {
                foreach (var group in groups)
                {
                    var entityId = group != null ? group.GetComponent<SceneEntityId>() : null;
                    if (entityId != null && entityId.HasGuid && entityId.Guid == record.groupGuid)
                        return group.transform;
                }

                Debug.LogWarning(
                    $"[WorldPlacementBake] 그룹 GUID '{record.groupGuid}'를 찾지 못했습니다. " +
                    "잘못된 동명 그룹에 복원하지 않도록 이름 폴백을 사용하지 않습니다.");
                return fallback;
            }

            if (string.IsNullOrEmpty(record.groupName))
                return fallback;

            MonsterGroupController matchedByName = null;
            int nameMatchCount = 0;
            foreach (var group in groups)
            {
                if (group == null || group.name != record.groupName)
                    continue;

                nameMatchCount++;
                matchedByName ??= group;
            }

            if (nameMatchCount > 1)
            {
                Debug.LogWarning(
                    $"[WorldPlacementBake] 이름이 '{record.groupName}'인 그룹이 {nameMatchCount}개라 복원 대상을 특정할 수 없습니다. " +
                    "그룹 오브젝트에 SceneEntityId를 추가하고 다시 Bake 하세요.");
                return fallback;
            }

            return matchedByName != null ? matchedByName.transform : fallback;
        }

        /// <summary>씬의 RuntimePlacementLoader가 참조 중인 PlacementData를 찾는다. 없으면 null.</summary>
        private static WorldPlacementDataSO FindExistingPlacementData(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var loader = root.GetComponentInChildren<RuntimePlacementLoader>(true);
                if (loader != null && loader.PlacementData != null)
                    return loader.PlacementData;
            }

            return null;
        }

        /// <summary>
        /// 신규 레코드를 기존 데이터에 병합한다. placementGuid가 같으면 갱신(순서 유지), 아니면 뒤에 추가.
        /// 기존 레코드는 어떤 경우에도 제거하지 않는다.
        /// </summary>
        private static void MergeRecords(
            WorldPlacementDataSO placementData,
            List<WorldPlacementRecord> newRecords,
            out int updatedCount,
            out int addedCount)
        {
            var newByGuid = new Dictionary<string, WorldPlacementRecord>();
            var noGuidRecords = new List<WorldPlacementRecord>();
            foreach (var record in newRecords)
            {
                if (string.IsNullOrEmpty(record.placementGuid))
                    noGuidRecords.Add(record);
                else
                    newByGuid[record.placementGuid] = record;
            }

            var merged = new List<WorldPlacementRecord>(placementData.Records.Count + newRecords.Count);
            updatedCount = 0;
            foreach (var record in placementData.Records)
            {
                if (record == null)
                    continue;

                if (!string.IsNullOrEmpty(record.placementGuid)
                    && newByGuid.TryGetValue(record.placementGuid, out var replacement))
                {
                    merged.Add(replacement);
                    newByGuid.Remove(record.placementGuid);
                    updatedCount++;
                }
                else
                {
                    merged.Add(record);
                }
            }

            merged.AddRange(newByGuid.Values);
            merged.AddRange(noGuidRecords);
            addedCount = newRecords.Count - updatedCount;

            placementData.SetRecords(merged);
        }

        public static void RestoreSelectedPlacementData()
        {
            var placementData = Selection.activeObject as WorldPlacementDataSO;
            if (placementData == null)
            {
                EditorUtility.DisplayDialog("PlacementData 복원", "Project 창에서 WorldPlacementDataSO를 선택하세요.", "확인");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("PlacementData 복원", "활성 씬이 유효하지 않습니다.", "확인");
                return;
            }

            var root = GameObject.Find("RestoredPlacementRoot");
            if (root == null)
            {
                root = new GameObject("RestoredPlacementRoot");
                Undo.RegisterCreatedObjectUndo(root, "Create Restored Placement Root");
            }

            ActorDatabase actorDatabase = null;
            int restored = 0;
            foreach (var record in placementData.Records)
            {
                if (record == null)
                    continue;

                // actorId 레코드는 prefab 참조가 비어 있으므로 ActorDatabase에서 역해석한다.
                bool isActorRecord = !string.IsNullOrEmpty(record.actorId);
                GameObject sourcePrefab = record.prefab;
                if (sourcePrefab == null && isActorRecord)
                {
                    actorDatabase ??= LoadActorDatabaseAsset();
                    if (actorDatabase != null && actorDatabase.TryGetDefinition(record.actorId, out var definition))
                        sourcePrefab = definition.prefab;
                }

                if (sourcePrefab == null && !CanCreateDefaultFromRecord(record))
                {
                    Debug.LogWarning($"[WorldPlacementBake] 레코드 '{record.actorId}{record.prefabId}'의 프리팹을 찾지 못해 복원을 건너뜁니다.", placementData);
                    continue;
                }

                GameObject instance;
                if (sourcePrefab != null)
                {
                    instance = PrefabUtility.InstantiatePrefab(sourcePrefab, scene) as GameObject;
                    if (instance == null)
                        instance = UnityEngine.Object.Instantiate(sourcePrefab);
                }
                else
                {
                    instance = CreateDefaultRestoreInstance(record);
                }

                Undo.RegisterCreatedObjectUndo(instance, "Restore Placement");
                Undo.SetTransformParent(instance.transform, ResolveRestoreParent(record, root.transform), "Restore Placement Parent");
                instance.transform.SetPositionAndRotation(record.position, record.rotation);
                instance.transform.localScale = record.scale;
                instance.SetActive(record.initiallyActive);
                ApplyRecordDataForRestore(record, instance);

                var metadata = instance.GetComponent<WorldPlacementMetadata>();
                if (metadata == null)
                    metadata = Undo.AddComponent<WorldPlacementMetadata>(instance);

                // RuntimeData 모드 + 원본 GUID/소스를 보존해야 재Bake 시 기존 레코드가 중복 추가 대신 제자리 갱신된다.
                metadata.EditorSetPlacementInfo(
                    isActorRecord
                        ? WorldPlacementMetadata.PlacementSourceKind.ActorDefinition
                        : ConvertSourceKind(record.sourceKind),
                    isActorRecord ? record.actorId : GetRestoreSourceId(record),
                    WorldPlacementMetadata.PlacementBakeMode.RuntimeData,
                    record.cellId,
                    record.randomSeed,
                    record.initiallyActive);
                metadata.EditorOverridePlacementGuid(record.placementGuid);
                EditorUtility.SetDirty(metadata);
                restored++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorUtility.DisplayDialog(
                "PlacementData 복원 완료",
                $"{restored}개 씬 오브젝트를 복원했습니다.\n\n레코드는 데이터에 그대로 남아 있으므로, 편집 후 재Bake하면 제자리 갱신됩니다.\n복원 상태로 PlayMode에 진입하면 로더가 같은 레코드를 또 생성하니 재Bake 후 진입하세요.",
                "확인");
        }

        public static void MarkSelectedAsRuntimeData()
        {
            SetSelectedBakeMode(WorldPlacementMetadata.PlacementBakeMode.RuntimeData);
        }

        public static void RegisterSelectedAsRuntimeData()
        {
            RegisterSelectedAsRuntimeDataInternal(false);
        }

        public static WorldPlacementDataSO RegisterSelectedAndBakeRuntimeData()
        {
            if (!RegisterSelectedAsRuntimeDataInternal(true))
                return null;

            return BakeOpenSceneRuntimeData();
        }

        public static void MarkSelectedAsSceneObject()
        {
            SetSelectedBakeMode(WorldPlacementMetadata.PlacementBakeMode.SceneObject);
        }

        private static bool RegisterSelectedAsRuntimeDataInternal(bool suppressSuccessDialog)
        {
            var selectedRoots = Selection.gameObjects;
            if (selectedRoots == null || selectedRoots.Length == 0)
            {
                EditorUtility.DisplayDialog("기존 배치 등록", "씬에서 Bake할 오브젝트를 선택하세요.", "확인");
                return false;
            }

            ActorDatabase actorDatabase = LoadActorDatabaseAsset();
            var targets = selectedRoots
                .Select(ResolvePlacementRoot)
                .Where(go => go != null && go.scene.IsValid())
                .Distinct()
                .ToArray();

            if (targets.Length == 0)
            {
                EditorUtility.DisplayDialog("기존 배치 등록", "선택 항목에 씬 오브젝트가 없습니다.", "확인");
                return false;
            }

            int registered = 0;
            int skipped = 0;
            foreach (var target in targets)
            {
                if (TryRegisterRuntimeDataTarget(target, actorDatabase))
                    registered++;
                else
                    skipped++;
            }

            if (registered > 0)
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            if (!suppressSuccessDialog || registered == 0)
            {
                string skipMessage = skipped > 0
                    ? $"\n\n건너뜀 {skipped}개: ActorDatabase 항목, 프리팹 원본, Gathering/DropItem 데이터 중 하나도 찾지 못했습니다."
                    : "";
                EditorUtility.DisplayDialog(
                    "기존 배치 등록",
                    $"{registered}개 씬 오브젝트를 RuntimeData Bake 대상으로 등록했습니다.{skipMessage}",
                    "확인");
            }

            return registered > 0;
        }

        private static GameObject ResolvePlacementRoot(GameObject selected)
        {
            if (selected == null)
                return null;

            var metadata = selected.GetComponentInParent<WorldPlacementMetadata>();
            if (metadata != null)
                return metadata.gameObject;

            var dropItem = selected.GetComponentInParent<DropItemActor>();
            if (dropItem != null)
                return dropItem.gameObject;

            var gathering = selected.GetComponentInParent<GatheringActor>();
            if (gathering != null)
                return gathering.gameObject;

            return PrefabUtility.GetNearestPrefabInstanceRoot(selected) ?? selected;
        }

        private static bool TryRegisterRuntimeDataTarget(GameObject target, ActorDatabase actorDatabase)
        {
            if (target == null || !target.scene.IsValid())
                return false;

            if (!TryResolvePlacementSource(target, actorDatabase, out var sourceKind, out string sourceId))
                return false;

            var metadata = target.GetComponent<WorldPlacementMetadata>();
            if (metadata == null)
                metadata = Undo.AddComponent<WorldPlacementMetadata>(target);
            else
                Undo.RecordObject(metadata, "Register RuntimeData Placement");

            metadata.EditorSetPlacementInfo(
                sourceKind,
                sourceId,
                WorldPlacementMetadata.PlacementBakeMode.RuntimeData,
                metadata.CellId,
                metadata.RandomSeed != 0 ? metadata.RandomSeed : UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                target.activeSelf);
            EditorUtility.SetDirty(metadata);

            var entityId = target.GetComponent<SceneEntityId>();
            if (entityId == null)
                entityId = Undo.AddComponent<SceneEntityId>(target);

            if (!entityId.HasGuid)
            {
                Undo.RecordObject(entityId, "Set SceneEntityId GUID");
                entityId.EditorSetGuid(Guid.NewGuid().ToString("N"));
                EditorUtility.SetDirty(entityId);
            }

            return true;
        }

        private static bool TryResolvePlacementSource(
            GameObject target,
            ActorDatabase actorDatabase,
            out WorldPlacementMetadata.PlacementSourceKind sourceKind,
            out string sourceId)
        {
            var dropItem = target.GetComponent<DropItemActor>();
            if (dropItem != null && dropItem.ItemData != null)
            {
                sourceKind = WorldPlacementMetadata.PlacementSourceKind.DropItemData;
                sourceId = GetAssetGuid(dropItem.ItemData);
                return true;
            }

            var gathering = target.GetComponent<GatheringActor>();
            if (gathering != null && gathering.GetData() != null)
            {
                sourceKind = WorldPlacementMetadata.PlacementSourceKind.GatheringData;
                sourceId = GetAssetGuid(gathering.GetData());
                return true;
            }

            var actor = target.GetComponent<GameActor>();
            if (actor != null
                && !string.IsNullOrEmpty(actor.ActorId)
                && actorDatabase != null
                && actorDatabase.TryGetDefinition(actor.ActorId, out _))
            {
                sourceKind = WorldPlacementMetadata.PlacementSourceKind.ActorDefinition;
                sourceId = actor.ActorId;
                return true;
            }

            GameObject prefab = ResolvePrefab(target);
            if (prefab != null && TryFindActorDefinition(actorDatabase, prefab, out var definition))
            {
                sourceKind = WorldPlacementMetadata.PlacementSourceKind.ActorDefinition;
                sourceId = definition.actorId;
                return true;
            }

            if (prefab != null)
            {
                sourceKind = WorldPlacementMetadata.PlacementSourceKind.DirectPrefab;
                sourceId = GetAssetGuid(prefab);
                return !string.IsNullOrEmpty(sourceId);
            }

            sourceKind = WorldPlacementMetadata.PlacementSourceKind.DirectPrefab;
            sourceId = "";
            return false;
        }

        private static bool TryFindActorDefinition(ActorDatabase actorDatabase, GameObject prefab, out ActorDefinitionSO definition)
        {
            definition = null;
            if (actorDatabase == null || prefab == null)
                return false;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            foreach (var candidate in actorDatabase.All)
            {
                if (candidate == null || candidate.prefab == null || string.IsNullOrEmpty(candidate.actorId))
                    continue;

                if (candidate.prefab == prefab)
                {
                    definition = candidate;
                    return true;
                }

                if (!string.IsNullOrEmpty(prefabPath)
                    && prefabPath == AssetDatabase.GetAssetPath(candidate.prefab))
                {
                    definition = candidate;
                    return true;
                }
            }

            return false;
        }

        private static void SetSelectedBakeMode(WorldPlacementMetadata.PlacementBakeMode bakeMode)
        {
            var targets = Selection.gameObjects
                .SelectMany(go => go.GetComponentsInChildren<WorldPlacementMetadata>(true))
                .Where(metadata => metadata != null)
                .Distinct()
                .ToArray();

            if (targets.Length == 0)
            {
                EditorUtility.DisplayDialog("Bake Mode 변경", "선택 오브젝트 하위에 WorldPlacementMetadata가 없습니다.", "확인");
                return;
            }

            foreach (var metadata in targets)
            {
                Undo.RecordObject(metadata, "Set Placement Bake Mode");
                metadata.EditorSetBakeMode(bakeMode);
                EditorUtility.SetDirty(metadata);
            }

            EditorUtility.DisplayDialog("Bake Mode 변경", $"{targets.Length}개 배치 메타를 {bakeMode}로 변경했습니다.", "확인");
        }

        private static List<WorldPlacementMetadata> CollectRuntimeDataTargets(Scene scene)
        {
            var targets = new List<WorldPlacementMetadata>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var metadata in root.GetComponentsInChildren<WorldPlacementMetadata>(true))
                {
                    if (metadata != null && metadata.BakeMode == WorldPlacementMetadata.PlacementBakeMode.RuntimeData)
                        targets.Add(metadata);
                }
            }

            return targets;
        }

        private static WorldPlacementRecord CreateRecord(WorldPlacementMetadata metadata, ActorDatabase actorDatabase)
        {
            WorldPlacementSourceKind sourceKind = ConvertSourceKind(metadata.SourceKind);
            // ActorDatabase에서 스폰 가능한 배치는 actorId 레코드로 저장한다 (prefab 참조 없음).
            bool isActorRecord =
                metadata.SourceKind == WorldPlacementMetadata.PlacementSourceKind.ActorDefinition
                && !string.IsNullOrEmpty(metadata.SourceId)
                && actorDatabase != null
                && actorDatabase.TryGetDefinition(metadata.SourceId, out _);

            if (metadata.SourceKind == WorldPlacementMetadata.PlacementSourceKind.ActorDefinition && !isActorRecord)
            {
                Debug.LogWarning(
                    $"[WorldPlacementBake] actorId '{metadata.SourceId}'를 ActorDatabase에서 찾지 못해 프리팹 직접 참조로 저장합니다.",
                    metadata);
            }

            GameObject prefab = null;
            if (!isActorRecord)
            {
                prefab = ResolvePrefab(metadata);
                if (prefab == null && !CanCreateDefaultFromMetadata(metadata))
                {
                    Debug.LogWarning($"[WorldPlacementBake] '{metadata.name}'의 프리팹을 찾지 못해 건너뜁니다.", metadata);
                    return null;
                }
            }

            var group = metadata.GetComponentInParent<MonsterGroupController>();
            var groupEntityId = group != null ? group.GetComponent<SceneEntityId>() : null;
            var groupPresetLink = group != null ? group.GetComponent<MonsterGroupPresetLink>() : null;
            var sceneEntityId = metadata.GetComponent<SceneEntityId>();
            var gatheringActor = metadata.GetComponent<GatheringActor>();
            var dropItemActor = metadata.GetComponent<DropItemActor>();

            Transform t = metadata.transform;
            return new WorldPlacementRecord
            {
                placementGuid = metadata.PlacementGuid,
                sceneEntityGuid = sceneEntityId != null && sceneEntityId.HasGuid
                    ? sceneEntityId.Guid
                    : metadata.PlacementGuid,
                sourceKind = sourceKind,
                prefabId = isActorRecord
                    ? ""
                    : string.IsNullOrEmpty(metadata.SourceId) ? GetAssetGuid(prefab) : metadata.SourceId,
                actorId = isActorRecord ? metadata.SourceId : "",
                prefab = prefab,
                interactableData = sourceKind == WorldPlacementSourceKind.GatheringData
                    ? gatheringActor != null ? gatheringActor.GetData() : null
                    : sourceKind == WorldPlacementSourceKind.DropItemData
                        ? dropItemActor != null ? dropItemActor.InteractionData : null
                        : null,
                itemData = sourceKind == WorldPlacementSourceKind.DropItemData && dropItemActor != null
                    ? dropItemActor.ItemData
                    : null,
                itemCount = sourceKind == WorldPlacementSourceKind.DropItemData && dropItemActor != null
                    ? dropItemActor.Count
                    : 1,
                position = t.position,
                rotation = t.rotation,
                scale = t.localScale,
                groupName = group != null ? group.name : "",
                groupGuid = groupEntityId != null && groupEntityId.HasGuid ? groupEntityId.Guid : "",
                groupPresetId = groupPresetLink != null ? groupPresetLink.PresetId : "",
                cellId = metadata.CellId,
                randomSeed = metadata.RandomSeed,
                initiallyActive = metadata.InitiallyActive,
            };
        }

        private static ActorDatabase LoadActorDatabaseAsset()
        {
            var guids = AssetDatabase.FindAssets("t:ActorDatabase");
            if (guids.Length == 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<ActorDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static GameObject ResolvePrefab(WorldPlacementMetadata metadata)
        {
            GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(metadata.gameObject);
            if (sourcePrefab != null)
                return sourcePrefab;

            if (!string.IsNullOrEmpty(metadata.SourceId))
            {
                string path = AssetDatabase.GUIDToAssetPath(metadata.SourceId);
                if (!string.IsNullOrEmpty(path))
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            return null;
        }

        private static GameObject ResolvePrefab(GameObject instance)
        {
            if (instance == null)
                return null;

            GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            if (sourcePrefab != null)
                return sourcePrefab;

            return PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance);
        }

        private static bool CanCreateDefaultFromMetadata(WorldPlacementMetadata metadata)
        {
            return metadata.SourceKind is WorldPlacementMetadata.PlacementSourceKind.GatheringData
                or WorldPlacementMetadata.PlacementSourceKind.DropItemData;
        }

        private static bool CanCreateDefaultFromRecord(WorldPlacementRecord record)
        {
            return record.sourceKind is WorldPlacementSourceKind.GatheringData
                or WorldPlacementSourceKind.DropItemData;
        }

        private static GameObject CreateDefaultRestoreInstance(WorldPlacementRecord record)
        {
            var instance = new GameObject(record.sourceKind == WorldPlacementSourceKind.GatheringData
                ? $"Gathering_{(record.interactableData != null ? record.interactableData.name : "Restored")}"
                : $"DropItem_{(record.itemData != null ? record.itemData.name : "Restored")}");
            return instance;
        }

        private static void ApplyRecordDataForRestore(WorldPlacementRecord record, GameObject instance)
        {
            var entityId = instance.GetComponent<SceneEntityId>();
            if (entityId == null)
                entityId = Undo.AddComponent<SceneEntityId>(instance);
            entityId.EditorSetGuid(!string.IsNullOrEmpty(record.sceneEntityGuid)
                ? record.sceneEntityGuid
                : record.placementGuid);
            EditorUtility.SetDirty(entityId);

            if (record.sourceKind == WorldPlacementSourceKind.GatheringData)
            {
                var gatheringActor = instance.GetComponent<GatheringActor>();
                if (gatheringActor == null)
                    gatheringActor = Undo.AddComponent<GatheringActor>(instance);
                gatheringActor.Init(record.interactableData);
                EditorUtility.SetDirty(gatheringActor);
                return;
            }

            if (record.sourceKind == WorldPlacementSourceKind.DropItemData)
            {
                var dropItemActor = instance.GetComponent<DropItemActor>();
                if (dropItemActor == null)
                    dropItemActor = Undo.AddComponent<DropItemActor>(instance);
                dropItemActor.Init(record.itemData, Mathf.Max(1, record.itemCount), record.interactableData);
                EditorUtility.SetDirty(dropItemActor);
            }
        }

        private static WorldPlacementSourceKind ConvertSourceKind(WorldPlacementMetadata.PlacementSourceKind sourceKind)
        {
            return sourceKind switch
            {
                WorldPlacementMetadata.PlacementSourceKind.ActorDefinition => WorldPlacementSourceKind.ActorDefinition,
                WorldPlacementMetadata.PlacementSourceKind.GatheringData => WorldPlacementSourceKind.GatheringData,
                WorldPlacementMetadata.PlacementSourceKind.DropItemData => WorldPlacementSourceKind.DropItemData,
                _ => WorldPlacementSourceKind.DirectPrefab,
            };
        }

        private static WorldPlacementMetadata.PlacementSourceKind ConvertSourceKind(WorldPlacementSourceKind sourceKind)
        {
            return sourceKind switch
            {
                WorldPlacementSourceKind.ActorDefinition => WorldPlacementMetadata.PlacementSourceKind.ActorDefinition,
                WorldPlacementSourceKind.GatheringData => WorldPlacementMetadata.PlacementSourceKind.GatheringData,
                WorldPlacementSourceKind.DropItemData => WorldPlacementMetadata.PlacementSourceKind.DropItemData,
                _ => WorldPlacementMetadata.PlacementSourceKind.DirectPrefab,
            };
        }

        private static string GetRestoreSourceId(WorldPlacementRecord record)
        {
            return record.sourceKind switch
            {
                WorldPlacementSourceKind.GatheringData => GetAssetGuid(record.interactableData),
                WorldPlacementSourceKind.DropItemData => GetAssetGuid(record.itemData),
                _ => record.prefabId,
            };
        }

        private static void EnsureRuntimeLoader(Scene scene, WorldPlacementDataSO placementData)
        {
            RuntimePlacementLoader loader = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                loader = root.GetComponentInChildren<RuntimePlacementLoader>(true);
                if (loader != null)
                    break;
            }

            if (loader == null)
            {
                var loaderObject = new GameObject(LoaderRootName);
                Undo.RegisterCreatedObjectUndo(loaderObject, "Create Runtime Placement Loader");
                loader = loaderObject.AddComponent<RuntimePlacementLoader>();
            }

            loader.EditorSetPlacementData(placementData);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static string GetAssetGuid(UnityEngine.Object asset)
        {
            if (asset == null)
                return "";

            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? "" : AssetDatabase.AssetPathToGUID(path);
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
    }
}
#endif
