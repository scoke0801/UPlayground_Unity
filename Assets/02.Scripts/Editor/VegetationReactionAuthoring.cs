using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Gameplay.World.LivingWorld;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.EditorTools
{
    /// <summary>명시 대응표의 식생만 기존 배치 루트에 연결하며 실패한 묶음은 Undo로 복원한다.</summary>
    public static class VegetationReactionAuthoring
    {
        private const float MinimumInputThickness = 0.001f;
        public sealed class Plant
        {
            public GameObject root;
            public ReactiveEnvironmentDefinitionSO definition;
            public List<Renderer> surfaces = new();
        }

        /// <summary>이름으로 가연성을 추론하지 않고 원본 메시 GUID로 전환 단위를 묶는다.</summary>
        public static List<Plant> Collect(Scene scene, VegetationReactionCatalogSO catalog)
        {
            if (!string.IsNullOrEmpty(catalog.sceneGuid) && AssetDatabase.AssetPathToGUID(scene.path) != catalog.sceneGuid)
                throw new InvalidOperationException("대응표의 씬 GUID와 현재 씬이 다릅니다.");
            var sources = new Dictionary<string, VegetationReactionCatalogSO.Source>();
            foreach (var source in catalog.sources) sources.Add(source.meshGuid, source);
            var plants = new Dictionary<GameObject, Plant>();
            var placements = new Dictionary<string, ReactiveEnvironmentDefinitionSO>();
            foreach (var placement in catalog.placements) placements.Add(placement.globalId, placement.definition);
            foreach (var sceneRoot in scene.GetRootGameObjects())
            foreach (var renderer in sceneRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(filter.sharedMesh));
                if (!sources.TryGetValue(guid, out var source) || source.definition == null) continue;
                var root = ResolveRoot(renderer);
                var actor = renderer.GetComponentInParent<GameActor>(true);
                if (actor != null && !(actor is GatheringActor)) continue;
                if (!plants.TryGetValue(root, out var plant))
                {
                    plant = new Plant { root = root, definition = actor is GatheringActor ? catalog.protectedVegetation : source.definition };
                    if (placements.TryGetValue(GlobalObjectId.GetGlobalObjectIdSlow(root).ToString(), out var definition)) plant.definition = definition;
                    plants.Add(root, plant);
                }
                plant.surfaces.Add(renderer);
            }
            return new List<Plant>(plants.Values);
        }

        private static GameObject ResolveRoot(Renderer renderer)
        {
            var actor = renderer.GetComponentInParent<GameActor>(true);
            GameObject result = null;
            for (var t = renderer.transform; t != null && (actor == null || t != actor.transform); t = t.parent)
                if (t.GetComponent<LODGroup>() != null) result = t.gameObject;
            var prefab = PrefabUtility.GetNearestPrefabInstanceRoot(renderer.gameObject);
            // 한 식물의 꽃·잎에 나란히 있는 LODGroup을 별도 연료로 분리하지 않는다.
            if (prefab != null && (result == null || result.transform.IsChildOf(prefab.transform))) result = prefab;
            if (result != null && (actor == null || result != actor.gameObject)) return result;
            if (actor != null && (result == null || result == actor.gameObject))
            {
                var child = renderer.transform;
                while (child.parent != null && child.parent != actor.transform) child = child.parent;
                return child.gameObject;
            }
            return renderer.gameObject;
        }

        /// <summary>사전 확인한 한 묶음만 연결한다. 저장은 전수 검사 후 호출자가 수행한다.</summary>
        public static int ApplyPlants(List<Plant> plants, VegetationReactionCatalogSO catalog, EnvironmentController weather)
        {
            int changed = 0;
            SceneEntityId.EditorInitializeNewIds(() => changed = ApplyPlantsCore(plants, catalog, weather));
            return changed;
        }

        private static int ApplyPlantsCore(List<Plant> plants, VegetationReactionCatalogSO catalog, EnvironmentController weather)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || weather == null) throw new InvalidOperationException("Edit Mode와 실제 날씨 연결이 필요합니다.");
            var materials = new Dictionary<Material, Material>();
            foreach (var surface in catalog.surfaces) materials.Add(surface.original, surface.reactive);
            foreach (var plant in plants)
            foreach (var renderer in plant.surfaces)
            foreach (var material in renderer.sharedMaterials)
                if (material == null || !ReactiveEnvironmentSurface.SupportsReaction(material) && !materials.ContainsKey(material))
                    throw new InvalidOperationException("재질 대응 누락: " + renderer.name);

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("식생 반응 연결");
            int changed = 0;
            try
            {
                foreach (var plant in plants)
                {
                    if (plant.root.GetComponent<ReactiveEnvironmentObject>() != null) continue;
                    ReactiveEnvironmentAuthoring.Apply(plant.root, plant.definition, weather);
                    var metadata = plant.root.GetComponent<WorldPlacementMetadata>();
                    if (metadata != null)
                    {
                        Undo.RecordObject(metadata, "저작 식생 씬 구성 보존");
                        metadata.EditorSetBakeMode(WorldPlacementMetadata.PlacementBakeMode.SceneObject);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(metadata);
                    }
                    var reaction = plant.root.GetComponent<ReactiveEnvironmentObject>();
                    var serialized = new SerializedObject(reaction);
                    var bindings = serialized.FindProperty("_surfaceBindings");
                    bindings.arraySize = plant.surfaces.Count;
                    var selected = new HashSet<Renderer>(plant.surfaces);
                    var excluded = new List<Renderer>();
                    foreach (var renderer in plant.root.GetComponentsInChildren<Renderer>(true))
                        if (!selected.Contains(renderer)) excluded.Add(renderer);
                    SetObjects(serialized.FindProperty("_excludedSurfaces"), excluded.ToArray());
                    for (int i = 0; i < plant.surfaces.Count; i++)
                    {
                        var renderer = plant.surfaces[i];
                        Undo.RecordObject(renderer, "식생 파생 재질 연결");
                        var slots = renderer.sharedMaterials;
                        for (int j = 0; j < slots.Length; j++)
                            if (materials.TryGetValue(slots[j], out var replacement)) slots[j] = replacement;
                        renderer.sharedMaterials = slots;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                        var binding = bindings.GetArrayElementAtIndex(i);
                        binding.FindPropertyRelative("renderer").objectReferenceValue = renderer;
                        var indices = binding.FindPropertyRelative("slots"); indices.arraySize = slots.Length;
                        for (int j = 0; j < slots.Length; j++) indices.GetArrayElementAtIndex(j).intValue = j;
                        binding.FindPropertyRelative("excludedSlots").arraySize = 0;
                    }
                    var identity = plant.root.GetComponent<SceneEntityId>();
                    if (!identity.HasGuid) { Undo.RecordObject(identity, "최초 식생 배치 ID"); identity.EditorSetGuid(Guid.NewGuid().ToString("N")); }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(identity);
                    // 기존 충돌은 보존하고 반응 입력 전용 trigger만 별도로 둔다.
                    var proxy = new GameObject("EnvironmentReactionHit");
                    Undo.RegisterCreatedObjectUndo(proxy, "식생 반응 입력 범위");
                    Undo.SetTransformParent(proxy.transform, plant.root.transform, "식생 입력 소유");
                    proxy.transform.localPosition = Vector3.zero;
                    proxy.transform.localRotation = Quaternion.identity;
                    proxy.transform.localScale = Vector3.one;
                    proxy.layer = LayerMask.NameToLayer("InteractableObject");
                    if (proxy.layer < 0) throw new InvalidOperationException("환경 피격 레이어가 없습니다.");
                    var collider = Undo.AddComponent<BoxCollider>(proxy);
                    collider.isTrigger = true;
                    Bounds local = LocalBounds(plant);
                    collider.center = local.center;
                    // 평면 메시도 물리 조회에 유효한 양의 부피를 갖게 한다.
                    collider.size = Vector3.Max(local.size, Vector3.one * MinimumInputThickness);
                    var hitColliders = new List<Collider>();
                    foreach (var candidate in plant.root.GetComponentsInChildren<Collider>(true))
                        if (candidate.GetComponentInParent<ReactiveEnvironmentObject>(true) == reaction) hitColliders.Add(candidate);
                    SetObjects(serialized.FindProperty("_hitColliders"), hitColliders.ToArray());
                    var samples = plant.root.transform.Find("ReactionSamples");
                    Undo.RecordObject(samples, "식생 표면 샘플");
                    samples.localPosition = local.center;
                    SetObjects(serialized.FindProperty("_burnAnchors"), new Transform[] { samples });
                    serialized.ApplyModifiedProperties();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(reaction);
                    changed++;
                }
                Undo.CollapseUndoOperations(group);
                return changed;
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        private static Bounds LocalBounds(Plant plant)
        {
            bool first = true;
            Bounds result = default;
            foreach (var renderer in plant.surfaces)
            {
                var filter = renderer.GetComponent<MeshFilter>();
                Bounds bounds = filter.sharedMesh.bounds;
                Matrix4x4 matrix = plant.root.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                    Vector3 point = matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents, sign));
                    if (first) { result = new Bounds(point, Vector3.zero); first = false; }
                    else result.Encapsulate(point);
                }
            }
            return result;
        }

        /// <summary>원본 TerrainData를 수정하지 않고 전체 레이어/구역의 런타임 어댑터를 저작한다.</summary>
        public static void ApplyTerrain(Terrain terrain, VegetationReactionCatalogSO catalog, EnvironmentController weather)
        {
            var data = terrain.terrainData;
            if (catalog.detailPrefabs.Length != data.detailPrototypes.Length) throw new InvalidOperationException("전체 Detail 레이어 대응이 필요합니다.");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                var identity = terrain.GetComponent<SceneEntityId>();
                if (identity == null) identity = Undo.AddComponent<SceneEntityId>(terrain.gameObject);
                if (!identity.HasGuid) identity.EditorSetGuid(Guid.NewGuid().ToString("N"));
                var reaction = terrain.GetComponent<TerrainDetailReaction>();
                if (reaction == null) reaction = Undo.AddComponent<TerrainDetailReaction>(terrain.gameObject);
                Undo.RecordObject(reaction, "전체 지형 식생 반응");
                var serialized = new SerializedObject(reaction);
                serialized.FindProperty("_definition").objectReferenceValue = catalog.terrainDefinition;
                serialized.FindProperty("_weather").objectReferenceValue = weather;
                int side = catalog.detailPixelsPerCell;
                if (data.detailWidth % side != 0 || data.detailHeight % side != 0) throw new InvalidOperationException("Detail 해상도가 셀 크기의 배수여야 합니다.");
                serialized.FindProperty("_detailPixelsPerCell").intValue = side;
                serialized.FindProperty("_cellRegion").rectIntValue = new RectInt(0, 0, data.detailWidth / side, data.detailHeight / side);
                serialized.FindProperty("_maximumCells").intValue = data.detailWidth / side * (data.detailHeight / side);
                serialized.FindProperty("_interactionHeight").floatValue = catalog.interactionHeight;
                serialized.FindProperty("_contentRevision").stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data)) + ":" + AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(data));
                var layers = serialized.FindProperty("_detailLayers"); layers.arraySize = catalog.detailPrefabs.Length;
                for (int i = 0; i < layers.arraySize; i++) layers.GetArrayElementAtIndex(i).intValue = i;
                SetObjects(serialized.FindProperty("_reactiveDetailPrefabs"), catalog.detailPrefabs);
                serialized.ApplyModifiedProperties();
                string error = reaction.ValidateAuthoring();
                if (error != null) throw new InvalidOperationException(error);
                Undo.CollapseUndoOperations(group);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        private static void SetObjects<T>(SerializedProperty array, T[] objects) where T : UnityEngine.Object
        {
            array.arraySize = objects.Length;
            for (int i = 0; i < objects.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
        }
    }
}
